using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Repositories;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class StatusHttpTests(SqlFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 14, 0, 0, TimeSpan.Zero);
    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task SwaggerPathIdUsesSingleIntegerType()
    {
        await using var factory = new RequestFactory(fixture.ConnectionString, Now);
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        // The bundled Swagger UI recognizes 3.1.x but rejects 3.0.4.
        Assert.Matches(@"^3\.1\.\d+$", document.GetProperty("openapi").GetString()!);
        var parameters = document.GetProperty("paths").GetProperty("/api/maintenance-requests/{id}/status")
            .GetProperty("patch").GetProperty("parameters");
        var id = parameters.EnumerateArray().Single(p => p.GetProperty("name").GetString() == "id");
        Assert.Equal("path", id.GetProperty("in").GetString());
        Assert.True(id.GetProperty("required").GetBoolean());
        Assert.Equal(JsonValueKind.String, id.GetProperty("schema").GetProperty("type").ValueKind);
        Assert.Equal("integer", id.GetProperty("schema").GetProperty("type").GetString());
    }

    private async Task<(int Id, int Tech)> PrepareAsync(byte status, bool visits = false)
    {
        await using var db = fixture.Create();
        var lease = await db.Leases.AsNoTracking().FirstAsync();
        var tech = await db.Technicians.Where(t => t.IsActive).Select(t => t.TechnicianId).FirstAsync();
        // SQL fixtures, not a visits endpoint or a bypass of business APIs.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.MaintenanceRequests(UnitId,TenantId,TechnicianId,Category,PriorityId,StatusId,Description,CreatedAt,ClosedAt) VALUES({lease.UnitId},{lease.TenantId},{(status == 1 ? (int?)null : tech)},N'T06 fixture',{(byte)1},{status},N'T06 isolated status fixture',{Now.AddDays(-1).UtcDateTime},{(status is 4 or 5 ? Now.AddHours(-1).UtcDateTime : (DateTime?)null)})");
        var id = await db.MaintenanceRequests.Where(r => r.Description == "T06 isolated status fixture")
            .OrderByDescending(r => r.RequestId).Select(r => r.RequestId).FirstAsync();
        if (visits) await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.Visits(RequestId,TechnicianId,VisitedAt,Notes,Cost) VALUES({id},{tech},{Now.AddHours(-2).UtcDateTime},N'T06 visit fixture',{0m})");
        return (id, tech);
    }
    private async Task CleanAsync(int id)
    {
        await using var db = fixture.Create();
        await db.Visits.Where(v => v.RequestId == id).ExecuteDeleteAsync();
        await db.MaintenanceRequests.Where(r => r.RequestId == id).ExecuteDeleteAsync();
    }

    [Theory]
    [InlineData(1, "Assigned", 2)]
    [InlineData(2, "InProgress", 3)]
    [InlineData(3, "Closed", 4)]
    [InlineData(1, "Cancelled", 5)]
    [InlineData(2, "Cancelled", 5)]
    public async Task PermittedTransitionPersistsExpectedFields(byte source, string target, byte expected)
    {
        var row = await PrepareAsync(source, visits: source == 3);
        try
        {
            await using var factory = new RequestFactory(fixture.ConnectionString, Now.AddTicks(12345));
            var body = target == "Assigned" ? $$"""{"status":"Assigned","technicianId":{{row.Tech}}}""" : $$"""{"status":"{{target}}"}""";
            var response = await factory.CreateClient().PatchAsync($"/api/maintenance-requests/{row.Id}/status", Json(body));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(target, result.GetProperty("status").GetString());
            Assert.False(result.TryGetProperty("rowVersion", out _));
            await using var db = fixture.Create();
            var saved = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(expected, saved.StatusId);
            Assert.Equal(source == 1 && expected == 5 ? null : (int?)row.Tech, saved.TechnicianId);
            if (expected is 4 or 5)
            {
                Assert.Equal(Now.UtcDateTime.AddMilliseconds(1), saved.ClosedAt);
                Assert.Equal(DateTimeKind.Utc, result.GetProperty("closedAt").GetDateTime().Kind);
            }
            else Assert.Null(saved.ClosedAt);
        }
        finally { await CleanAsync(row.Id); }
    }

    [Theory]
    [InlineData("missing-request", 404, "REQUEST_NOT_FOUND")]
    [InlineData("missing-tech", 404, "TECHNICIAN_NOT_FOUND")]
    [InlineData("inactive-tech", 409, "TECHNICIAN_INACTIVE")]
    [InlineData("invalid-transition", 409, "INVALID_STATUS_TRANSITION")]
    [InlineData("no-visits", 409, "VISIT_REQUIRED")]
    [InlineData("missing-status", 400, null)]
    [InlineData("invalid-status", 400, null)]
    [InlineData("numeric-status", 400, null)]
    [InlineData("no-tech", 400, null)]
    [InlineData("reassign", 400, null)]
    [InlineData("invalid-tech-id", 400, null)]
    [InlineData("invalid-id", 400, null)]
    [InlineData("invalid-id-format", 400, null)]
    [InlineData("server-field", 400, null)]
    [InlineData("same-status", 409, "INVALID_STATUS_TRANSITION")]
    [InlineData("reopen", 409, "INVALID_STATUS_TRANSITION")]
    public async Task RejectedTransitionDoesNotChangePersistedRow(string scenario, int status, string? code)
    {
        var row = await PrepareAsync(scenario == "no-visits" ? (byte)3 : scenario == "reopen" ? (byte)5 : (byte)1);
        try
        {
            await using var db = fixture.Create();
            var before = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            var tech = scenario == "missing-tech" ? int.MaxValue : scenario == "inactive-tech"
                ? await db.Technicians.Where(t => !t.IsActive).Select(t => t.TechnicianId).FirstAsync() : row.Tech;
            var body = scenario switch
            {
                "no-visits" or "invalid-transition" => "{\"status\":\"Closed\"}",
                "missing-status" => "{}", "invalid-status" => "{\"status\":\"Unknown\"}",
                "numeric-status" => "{\"status\":2,\"technicianId\":1}",
                "no-tech" => "{\"status\":\"Assigned\"}",
                "reassign" => $$"""{"status":"Cancelled","technicianId":{{tech}}}""",
                "invalid-tech-id" => "{\"status\":\"Assigned\",\"technicianId\":0}",
                "server-field" => "{\"status\":\"Cancelled\",\"closedAt\":\"2000-01-01\"}",
                "same-status" or "reopen" => "{\"status\":\"Open\"}",
                _ => $$"""{"status":"Assigned","technicianId":{{tech}}}"""
            };
            await using var factory = new RequestFactory(fixture.ConnectionString, Now);
            var id = scenario == "missing-request" ? int.MaxValue : scenario == "invalid-id" ? 0 : row.Id;
            var pathId = scenario == "invalid-id-format" ? "abc" : id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var response = await factory.CreateClient().PatchAsync($"/api/maintenance-requests/{pathId}/status", Json(body));
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (code is not null) Assert.Equal(code, problem.GetProperty("code").GetString());
            var after = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(before.RowVersion, after.RowVersion); Assert.Equal(before.StatusId, after.StatusId);
            Assert.Equal(before.TechnicianId, after.TechnicianId); Assert.Equal(before.ClosedAt, after.ClosedAt);
            Assert.Equal(before.CreatedAt, after.CreatedAt);
        }
        finally { await CleanAsync(row.Id); }
    }

    private sealed class Gate
    {
        public TaskCompletionSource Read { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Spid;
        public byte[] OriginalVersion = [];
        public Exception? SaveError;
    }
    private sealed class GatedRequests(PropertyMaintenanceDbContext db, Gate gate) : IMaintenanceRequestRepository
    {
        private readonly MaintenanceRequestRepository inner = new(db);
        public async Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var model = await inner.GetByIdAsync(id, cancellationToken);
            gate.OriginalVersion = model!.RowVersion.ToArray();
            gate.Spid = await db.Database.SqlQuery<int>($"SELECT CONVERT(int,@@SPID) AS Value").SingleAsync(cancellationToken);
            gate.Read.TrySetResult();
            await gate.Continue.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return model;
        }
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => inner.HasVisitsAsync(id, cancellationToken);
        public Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => inner.AddAsync(model, cancellationToken);
        public async Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
        {
            Assert.Equal(gate.OriginalVersion, model.RowVersion);
            try { return await inner.UpdateStatusAsync(model, cancellationToken); }
            catch (Exception error) { gate.SaveError = error; throw; }
        }
    }

    [Fact]
    public async Task IndependentTransactionsProduceRowVersionConflictAndHttp409()
    {
        var row = await PrepareAsync(1);
        var gate = new Gate(); Task<HttpResponseMessage>? losing = null;
        await using var baseFactory = new RequestFactory(fixture.ConnectionString, Now);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMaintenanceRequestRepository>();
            services.AddScoped<IMaintenanceRequestRepository>(sp => new GatedRequests(sp.GetRequiredService<PropertyMaintenanceDbContext>(), gate));
        }));
        try
        {
            losing = factory.CreateClient().PatchAsync($"/api/maintenance-requests/{row.Id}/status",
                Json($$"""{"status":"Assigned","technicianId":{{row.Tech}}}"""));
            await gate.Read.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await using (var winner = fixture.Create())
            {
                await using var transaction = await winner.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                var spid = await winner.Database.SqlQuery<int>($"SELECT CONVERT(int,@@SPID) AS Value").SingleAsync();
                Assert.NotEqual(gate.Spid, spid);
                var repository = new MaintenanceRequestRepository(winner);
                var original = (await repository.GetByIdAsync(row.Id))!;
                Assert.Equal(gate.OriginalVersion, original.RowVersion);
                original.StatusId = 5; original.ClosedAt = Now.UtcDateTime;
                var saved = await repository.UpdateStatusAsync(original);
                Assert.False(gate.OriginalVersion.SequenceEqual(saved.RowVersion));
                await transaction.CommitAsync();
            }
            gate.Continue.TrySetResult();
            var response = await losing;
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.IsType<DbUpdateConcurrencyException>(gate.SaveError);
            Assert.Equal("CONCURRENCY_CONFLICT", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            await using var audit = fixture.Create();
            var persisted = await audit.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(5, persisted.StatusId); Assert.Null(persisted.TechnicianId);
            Assert.Equal(Now.UtcDateTime, persisted.ClosedAt);
        }
        finally
        {
            gate.Continue.TrySetResult();
            if (losing != null) { try { await losing; } catch { /* Cleanup without hiding the original assertion. */ } }
            await CleanAsync(row.Id);
        }
    }
}
