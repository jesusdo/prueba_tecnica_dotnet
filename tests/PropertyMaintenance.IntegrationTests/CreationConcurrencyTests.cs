using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class CreationConcurrencyTests(SqlFixture fixture)
{
    private sealed class GatedLease(ILeaseRepository inner) : ILeaseRepository
    {
        public TaskCompletionSource Read { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default)
        {
            var result = await inner.HasActiveLeaseAsync(unitId, tenantId, date, cancellationToken);
            Read.TrySetResult();
            await Continue.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return result;
        }
    }
    private sealed class FailingRequests(IMaintenanceRequestRepository inner) : IMaintenanceRequestRepository
    {
        public Task<PagedResult<RequestReadModel>> ListAsync(RequestFilter filter, CancellationToken cancellationToken = default) => inner.ListAsync(filter, cancellationToken);
        public Task LockForUpdateAsync(int id, CancellationToken cancellationToken = default) => inner.LockForUpdateAsync(id, cancellationToken);
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => inner.HasVisitsAsync(id, cancellationToken);
        public Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => inner.UpdateStatusAsync(model, cancellationToken);
        public Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public async Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
        {
            await inner.AddAsync(model, cancellationToken);
            throw new InvalidOperationException("Isolated failure after SaveChanges.");
        }
    }

    [Fact]
    public async Task IndependentConnectionCannotInvalidateLeaseBetweenValidationAndInsert()
    {
        await using var db = fixture.Create();
        var lease = await db.Leases.AsNoTracking().FirstAsync(l => l.EndDate == null);
        var gate = new GatedLease(new LeaseRepository(db));
        var service = new MaintenanceRequestService(new UnitRepository(db), new TenantRepository(db), gate,
            new MaintenanceRequestRepository(db), TimeProvider.System, new TechnicianRepository(db));
        Task<MaintenanceRequestModel>? creation = null;
        int? createdId = null;
        try
        {
            creation = service.CreateAsync(lease.UnitId, lease.TenantId, "T05 concurrency", RequestPriorityId.Low, "T05 concurrent fixture");
            await gate.Read.Task.WaitAsync(TimeSpan.FromSeconds(15));
            // Explicitly disable ambient enlistment: this is a second physical
            // SQL session, not the creator's transaction or connection.
            var config = new SqlConnectionStringBuilder(fixture.ConnectionString) { Enlist = false };
            await using var connection = new SqlConnection(config.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SET LOCK_TIMEOUT 1000; UPDATE dbo.Leases SET EndDate=StartDate WHERE LeaseId=@id;";
            command.Parameters.AddWithValue("@id", lease.LeaseId);
            var rejected = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(1222, rejected.Number);
            gate.Continue.TrySetResult();
            var saved = await creation;
            createdId = saved.RequestId;
            Assert.True(saved.RequestId > 0);
            await using var audit = fixture.Create();
            Assert.Null(await audit.Leases.Where(l => l.LeaseId == lease.LeaseId).Select(l => l.EndDate).SingleAsync());
            Assert.True(await audit.MaintenanceRequests.AnyAsync(r => r.RequestId == saved.RequestId));
        }
        finally
        {
            gate.Continue.TrySetResult();
            if (creation != null)
            {
                try { createdId ??= (await creation).RequestId; } catch { /* Preserve original test failure; transaction disposes. */ }
            }
            await using var cleanup = fixture.Create();
            if (createdId.HasValue) await cleanup.MaintenanceRequests.Where(r => r.RequestId == createdId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task FailureAfterSaveRollsBackEntireCreation()
    {
        await using var db = fixture.Create();
        var lease = await db.Leases.AsNoTracking().FirstAsync(l => l.EndDate == null);
        var before = await db.MaintenanceRequests.CountAsync();
        var service = new MaintenanceRequestService(new UnitRepository(db), new TenantRepository(db), new LeaseRepository(db),
            new FailingRequests(new MaintenanceRequestRepository(db)), TimeProvider.System, new TechnicianRepository(db));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(lease.UnitId, lease.TenantId,
            "T05 rollback", RequestPriorityId.Low, "T05 rollback fixture"));
        Assert.Equal("Isolated failure after SaveChanges.", error.Message);
        await using var audit = fixture.Create();
        Assert.Equal(before, await audit.MaintenanceRequests.CountAsync());
        Assert.False(await audit.MaintenanceRequests.AnyAsync(r => r.Description == "T05 rollback fixture"));
    }
}
