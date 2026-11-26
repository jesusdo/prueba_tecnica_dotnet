using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Migrations;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class ReportMigrationTests(SqlFixture fixture)
{
    [Fact]
    public async Task EmbeddedProcedureBytesMatchCanonicalManualFiles()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "database", "scripts"))) root = root.Parent;
        Assert.NotNull(root);
        foreach (var pair in new[] {
            ("dbo.usp_GetPropertiesSummary.sql", "006_CreatePropertiesSummaryProcedure.sql"),
            ("dbo.usp_GetTechniciansWorkload.sql", "007_CreateTechniciansWorkloadProcedure.sql") })
        {
            await using var stream = typeof(DatabaseMigrator).Assembly.GetManifestResourceStream(
                "PropertyMaintenance.Api.database.migrations." + pair.Item2);
            Assert.NotNull(stream);
            using var memory = new MemoryStream(); await stream.CopyToAsync(memory);
            Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(root.FullName, "database", "scripts", pair.Item1)), memory.ToArray());
        }
    }

    [Fact]
    public async Task Decimal38OutsideClrRangeFailsInsteadOfSilentlyTruncating()
    {
        await using var db = fixture.Create();
        await Assert.ThrowsAsync<OverflowException>(() => db.Database.SqlQuery<MoneyProbe>($"""
            SELECT CAST(999999999999999999999999999999.99 AS decimal(38,2)) AS Amount
            """).ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanAndExistingDatabasesReceiveProceduresOnceAndReturnEmptyReports(bool existing)
    {
        // Only new, explicitly test-named databases are created; none is dropped or overwritten.
        var name = "PropertyMaintenanceDb_T04_T09_" + (existing ? "Existing_" : "Clean_") + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(fixture.ConnectionString) { InitialCatalog = "master" };
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync();
            await using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{name}]";
            await create.ExecuteNonQueryAsync();
        }
        builder.InitialCatalog = name;
        var connection = builder.ConnectionString;
        var assembly = typeof(DatabaseMigrator).Assembly;
        if (existing)
        {
            // Actually run 001–005 with DbUp, including its own journal. No fabricated baseline.
            var baseline = DeployChanges.To.SqlDatabase(connection)
                .WithScriptsEmbeddedInAssembly(assembly, n => n.StartsWith("PropertyMaintenance.Api.database.migrations.")
                    && string.CompareOrdinal(n, "PropertyMaintenance.Api.database.migrations.006") < 0)
                .JournalToSqlTable("dbo", "SchemaVersions").WithTransactionPerScript().Build().PerformUpgrade();
            Assert.True(baseline.Successful, baseline.Error?.GetType().Name);
        }
        var migrator = new DatabaseMigrator(NullLogger<DatabaseMigrator>.Instance);
        migrator.Apply(connection);
        await using var sql = new SqlConnection(connection); await sql.OpenAsync();
        async Task<string> Snapshot()
        {
            await using var command = sql.CreateCommand();
            command.CommandText = "SELECT Id,ScriptName,Applied FROM dbo.SchemaVersions ORDER BY Id FOR JSON PATH";
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var first = await Snapshot();
        using (var json = System.Text.Json.JsonDocument.Parse(first))
        {
            Assert.Equal(7, json.RootElement.GetArrayLength());
            Assert.Contains("006_CreatePropertiesSummaryProcedure", first);
            Assert.Contains("007_CreateTechniciansWorkloadProcedure", first);
        }
        migrator.Apply(connection);
        Assert.Equal(first, await Snapshot());
        var options = new DbContextOptionsBuilder<PropertyMaintenanceDbContext>().UseSqlServer(connection).Options;
        await using var db = new PropertyMaintenanceDbContext(options);
        Assert.Empty(await db.Database.SqlQuery<int>($"SELECT PropertyId AS Value FROM dbo.Properties").ToArrayAsync());
        await using var factory = new RequestFactory(connection, ReadFixtures.Now);
        foreach (var url in new[] { "/api/reports/properties-summary?from=2026-01-01&to=2026-12-31", "/api/reports/technicians-workload" })
        {
            var response = await factory.CreateClient().GetAsync(url);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        }
    }
}

internal sealed class MoneyProbe
{
    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(38,2)")]
    public decimal Amount { get; set; }
}
