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
        var name = "PropertyMaintenanceDb_T04_T102_" + (existing ? "Existing_" : "Clean_") + Guid.NewGuid().ToString("N");
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
        var definitionsBefore = new Dictionary<string, byte[]>();
        var procedures = new[] { "dbo.usp_GetPropertiesSummary", "dbo.usp_GetTechniciansWorkload" };
        async Task<byte[]> Definition(string procedure)
        {
            await using var reader = new SqlConnection(connection);
            await reader.OpenAsync();
            await using var command = reader.CreateCommand();
            command.CommandText = "SELECT CONVERT(varbinary(max),OBJECT_DEFINITION(OBJECT_ID(@procedure)))";
            command.Parameters.AddWithValue("@procedure", procedure);
            return Assert.IsType<byte[]>(await command.ExecuteScalarAsync());
        }
        if (existing)
        {
            // Actually run 001–007, including the untouched manual procedures and journal.
            var baseline = DeployChanges.To.SqlDatabase(connection)
                .WithScriptsEmbeddedInAssembly(assembly, n => n.StartsWith("PropertyMaintenance.Api.database.migrations.")
                    && string.CompareOrdinal(n, "PropertyMaintenance.Api.database.migrations.008") < 0)
                .JournalToSqlTable("dbo", "SchemaVersions").WithTransactionPerScript().Build().PerformUpgrade();
            Assert.True(baseline.Successful, baseline.Error?.GetType().Name);
            foreach (var procedure in procedures) definitionsBefore[procedure] = await Definition(procedure);
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
            Assert.Equal(8, json.RootElement.GetArrayLength());
            Assert.Contains("006_CreatePropertiesSummaryProcedure", first);
            Assert.Contains("007_CreateTechniciansWorkloadProcedure", first);
            Assert.Contains("008_FixLeaseOverlapTrigger", first);
        }
        foreach (var procedure in procedures)
        {
            var definition = await Definition(procedure);
            if (existing) Assert.Equal(definitionsBefore[procedure], definition);
            else definitionsBefore[procedure] = definition;
        }
        migrator.Apply(connection);
        Assert.Equal(first, await Snapshot());
        foreach (var procedure in procedures) Assert.Equal(definitionsBefore[procedure], await Definition(procedure));
        await using (var validation = sql.CreateCommand())
        {
            validation.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name=N'DemoSeedVersions'";
            Assert.Equal(0, (int)(await validation.ExecuteScalarAsync())!);
            validation.CommandText = SchemaValidationTests.ReadScript();
            Assert.StartsWith("PASS:", Assert.IsType<string>(await validation.ExecuteScalarAsync()));
        }
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
