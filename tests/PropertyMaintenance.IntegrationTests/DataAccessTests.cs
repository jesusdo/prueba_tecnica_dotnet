using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.DataAccess.Migrations;
using PropertyMaintenance.Api.DataAccess.Repositories;

namespace PropertyMaintenance.IntegrationTests;

public sealed class SqlFixture
{
    public string ConnectionString { get; }
    public SqlFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("PROPERTY_MAINTENANCE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set PROPERTY_MAINTENANCE_TEST_CONNECTION to an isolated local T04 database.");
        var connection = new SqlConnectionStringBuilder(ConnectionString);
        if (!connection.InitialCatalog.StartsWith("PropertyMaintenanceDb_T04_", StringComparison.Ordinal)
            || !(connection.DataSource.StartsWith("db,", StringComparison.OrdinalIgnoreCase)
                || connection.DataSource.StartsWith("localhost,", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Tests require a local PropertyMaintenanceDb_T04_ database.");
        var migrator = new DatabaseMigrator(NullLogger<DatabaseMigrator>.Instance);
        migrator.Apply(ConnectionString);
        migrator.ApplyDemoSeed(ConnectionString);
    }
    public PropertyMaintenanceDbContext Create() => new(new DbContextOptionsBuilder<PropertyMaintenanceDbContext>()
        .UseSqlServer(ConnectionString).Options);
}

[CollectionDefinition("SQL", DisableParallelization = true)]
public sealed class SqlCollection : ICollectionFixture<SqlFixture> { }

[Collection("SQL")]
public sealed class DataAccessTests(SqlFixture fixture)
{
    [Fact]
    public async Task ConnectsToSqlServer()
    {
        await using var db = fixture.Create();
        Assert.True(await db.Database.CanConnectAsync());
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
    }

    [Fact]
    public async Task LoadsEntitiesAndRelationships()
    {
        await using var db = fixture.Create();
        var unit = await db.Units.AsNoTracking().Include(u => u.Property).Include(u => u.UnitType)
            .Include(u => u.Leases).ThenInclude(l => l.Tenant).FirstAsync();
        Assert.NotEmpty(unit.Property.Name);
        Assert.NotEmpty(unit.UnitType.Code);
        Assert.NotEmpty(unit.Leases);
        Assert.All(unit.Leases, l => Assert.NotEmpty(l.Tenant.Email));
    }

    [Theory]
    [InlineData("current", true)]
    [InlineData("expired", false)]
    [InlineData("future", false)]
    [InlineData("inclusive", true)]
    [InlineData("open-ended", true)]
    [InlineData("wrong-tenant", false)]
    public async Task LeaseQueryRespectsDatesAndTenant(string scenario, bool expected)
    {
        await using var db = fixture.Create();
        var reference = await db.MaintenanceRequests.OrderBy(r => r.RequestId).Select(r => r.Description).FirstAsync();
        var today = DateOnly.ParseExact(reference.Substring(12, 10), "yyyy-MM-dd");
        var query = db.Leases.AsNoTracking();
        var lease = scenario switch
        {
            "expired" => await query.FirstAsync(l => l.EndDate < today),
            "future" => await query.FirstAsync(l => l.StartDate > today),
            "inclusive" => await query.FirstAsync(l => l.StartDate <= today && l.EndDate >= today),
            _ => await query.FirstAsync(l => l.EndDate == null)
        };
        var tenantId = scenario == "wrong-tenant" ? await db.Tenants.Where(t => t.TenantId != lease.TenantId)
            .Select(t => t.TenantId).FirstAsync() : lease.TenantId;
        // Historical tenants renew on the same unit. Probe the gap after the
        // expired lease, rather than accidentally finding its valid renewal.
        var date = scenario == "inclusive" ? lease.EndDate!.Value
            : scenario == "expired" ? lease.EndDate!.Value.AddDays(1) : today;
        Assert.Equal(expected, await new LeaseRepository(db).HasActiveLeaseAsync(lease.UnitId, tenantId, date));
        if (scenario == "inclusive")
            Assert.True(await new LeaseRepository(db).HasActiveLeaseAsync(lease.UnitId, tenantId, lease.StartDate));
    }

    [Fact]
    public async Task FindsExistingRequestWithGeneratedValues()
    {
        await using var db = fixture.Create();
        var id = await db.MaintenanceRequests.Select(r => r.RequestId).FirstAsync();
        var model = await new MaintenanceRequestRepository(db).GetByIdAsync(id);
        Assert.NotNull(model);
        Assert.Equal(id, model.RequestId);
        Assert.Equal(8, model.RowVersion.Length);
        Assert.Equal(DateTimeKind.Utc, model.CreatedAt.Kind);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task MissingRequestReturnsNull()
    {
        await using var db = fixture.Create();
        Assert.Null(await new MaintenanceRequestRepository(db).GetByIdAsync(-1));
    }

    [Fact]
    public async Task InsertsRequestAndReadsSqlGeneratedValues()
    {
        await using var db = fixture.Create();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var lease = await db.Leases.FirstAsync(l => l.EndDate == null);
        var before = DateTime.UtcNow.AddSeconds(-1);
        var saved = await new MaintenanceRequestRepository(db).AddAsync(new MaintenanceRequestModel
        {
            UnitId = lease.UnitId, TenantId = lease.TenantId, PriorityId = 2,
            Category = "T04 fixture", Description = "T04 generated values"
        });
        Assert.True(saved.RequestId > 0);
        Assert.Equal(1, saved.StatusId);
        Assert.Null(saved.TechnicianId);
        Assert.Null(saved.ClosedAt);
        Assert.Equal(8, saved.RowVersion.Length);
        Assert.InRange(saved.CreatedAt, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        var persisted = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == saved.RequestId);
        Assert.Equal(saved.RowVersion, persisted.RowVersion);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task IndependentContextsDetectRealRowVersionConflict()
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var options = new DbContextOptionsBuilder<PropertyMaintenanceDbContext>().UseSqlServer(connection).Options;
        await using var first = new PropertyMaintenanceDbContext(options);
        await using var second = new PropertyMaintenanceDbContext(options);
        await first.Database.UseTransactionAsync(transaction);
        await second.Database.UseTransactionAsync(transaction);
        var a = await first.MaintenanceRequests.OrderBy(r => r.RequestId).FirstAsync();
        var b = await second.MaintenanceRequests.SingleAsync(r => r.RequestId == a.RequestId);
        var oldVersion = a.RowVersion.ToArray();
        a.Description = "T04 first writer";
        await first.SaveChangesAsync();
        Assert.False(oldVersion.SequenceEqual(a.RowVersion));
        b.Description = "T04 stale writer";
        var error = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        Assert.Single(error.Entries);
        Assert.Equal(a.RequestId, ((MaintenanceRequest)error.Entries[0].Entity).RequestId);
        first.ChangeTracker.Clear();
        Assert.Equal("T04 first writer", await first.MaintenanceRequests.Where(r => r.RequestId == a.RequestId)
            .Select(r => r.Description).SingleAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ValidLeaseUpdateWorksWithEnabledTrigger()
    {
        await using var db = fixture.Create();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var lease = await db.Leases.FirstAsync();
        lease.MonthlyRent += 1;
        await db.SaveChangesAsync();
        Assert.Equal(lease.MonthlyRent, await db.Leases.AsNoTracking().Where(l => l.LeaseId == lease.LeaseId)
            .Select(l => l.MonthlyRent).SingleAsync());
        var enabled = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.triggers WHERE name = N'TR_Leases_PreventOverlap' AND is_disabled = 0").SingleAsync();
        Assert.Equal(1, enabled);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task OverlapIsRejectedWithSql51021()
    {
        await using var db = fixture.Create();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var existing = await db.Leases.AsNoTracking().FirstAsync();
        db.Leases.Add(new Lease { UnitId = existing.UnitId, TenantId = existing.TenantId,
            StartDate = existing.StartDate, EndDate = existing.EndDate, MonthlyRent = 1 });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Equal(51021, sql.Number);
        Assert.Contains("overlaps", sql.Message);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ModelMatchesSqlTypesAndConcurrency()
    {
        using var db = fixture.Create();
        Assert.Equal(11, db.Model.GetEntityTypes().Count());
        Assert.All(db.Model.GetEntityTypes(), e => Assert.NotNull(e.FindPrimaryKey()));
        Assert.All(db.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()), fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
        var lease = db.Model.FindEntityType(typeof(Lease))!;
        Assert.Equal("date", lease.FindProperty(nameof(Lease.StartDate))!.GetColumnType());
        Assert.True(lease.FindProperty(nameof(Lease.EndDate))!.IsNullable);
        Assert.Equal("decimal(18,2)", lease.FindProperty(nameof(Lease.MonthlyRent))!.GetColumnType()!.Replace(" ", ""));
        var request = db.Model.FindEntityType(typeof(MaintenanceRequest))!;
        Assert.Equal(3, request.FindProperty(nameof(MaintenanceRequest.CreatedAt))!.GetPrecision());
        Assert.True(request.FindProperty(nameof(MaintenanceRequest.TechnicianId))!.IsNullable);
        Assert.Equal(100, request.FindProperty(nameof(MaintenanceRequest.Category))!.GetMaxLength());
        var version = request.FindProperty(nameof(MaintenanceRequest.RowVersion))!;
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
        Assert.Equal(typeof(byte), request.FindProperty(nameof(MaintenanceRequest.StatusId))!.ClrType);
        Assert.Equal((byte)1, request.FindProperty(nameof(MaintenanceRequest.StatusId))!.GetDefaultValue());
        Assert.Contains("sysutcdatetime", request.FindProperty(nameof(MaintenanceRequest.CreatedAt))!.GetDefaultValueSql()!);
        Assert.Equal(3, request.FindProperty(nameof(MaintenanceRequest.ClosedAt))!.GetPrecision());
        var visit = db.Model.FindEntityType(typeof(Visit))!;
        Assert.Equal(3, visit.FindProperty(nameof(Visit.VisitedAt))!.GetPrecision());
        Assert.Equal("decimal(18,2)", visit.FindProperty(nameof(Visit.Cost))!.GetColumnType()!.Replace(" ", ""));
        Assert.Equal(200, db.Model.FindEntityType(typeof(Property))!.FindProperty(nameof(Property.Name))!.GetMaxLength());
        Assert.Equal(320, db.Model.FindEntityType(typeof(Tenant))!.FindProperty(nameof(Tenant.Email))!.GetMaxLength());
        Assert.Equal(new byte[] {1,2,3,4,5}, await db.RequestStatuses.OrderBy(s => s.StatusId).Select(s => s.StatusId).ToArrayAsync());
        Assert.Equal(new byte[] {1,2,3,4}, await db.RequestPriorities.OrderBy(s => s.PriorityId).Select(s => s.PriorityId).ToArrayAsync());
        Assert.Equal(new byte[] {1,2}, await db.UnitTypes.OrderBy(s => s.UnitTypeId).Select(s => s.UnitTypeId).ToArrayAsync());
        Assert.Equal(new byte[] {1,2,3}, await db.TechnicianSpecialties.OrderBy(s => s.SpecialtyId).Select(s => s.SpecialtyId).ToArrayAsync());
    }
}
