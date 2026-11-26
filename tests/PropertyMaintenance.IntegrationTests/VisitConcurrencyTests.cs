using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.DataAccess.Repositories;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class VisitConcurrencyTests(SqlFixture fixture)
{
    private sealed class Gate
    {
        public int Spid;
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class GatedVisit(PropertyMaintenanceDbContext db, Gate gate, bool failAfterSave = false) : IVisitRepository
    {
        public async Task<VisitModel> AddAsync(VisitModel model, CancellationToken cancellationToken = default)
        {
            gate.Spid = await db.Database.SqlQuery<int>($"SELECT CONVERT(int,@@SPID) AS Value").SingleAsync(cancellationToken);
            gate.Ready.TrySetResult();
            await gate.Continue.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            var saved = await new VisitRepository(db).AddAsync(model, cancellationToken);
            if (failAfterSave) throw new InvalidOperationException("T07 isolated failure after visit insertion.");
            return saved;
        }
    }
    private sealed class ObservedRequests(PropertyMaintenanceDbContext db, Gate gate) : IMaintenanceRequestRepository
    {
        private readonly MaintenanceRequestRepository inner = new(db);
        public async Task LockForUpdateAsync(int id, CancellationToken cancellationToken = default)
        {
            gate.Spid = await db.Database.SqlQuery<int>($"SELECT CONVERT(int,@@SPID) AS Value").SingleAsync(cancellationToken);
            gate.Ready.TrySetResult();
            await inner.LockForUpdateAsync(id, cancellationToken);
        }
        public Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);
        public Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => inner.AddAsync(model, cancellationToken);
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => inner.HasVisitsAsync(id, cancellationToken);
        public Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => inner.UpdateStatusAsync(model, cancellationToken);
    }
    private async Task AssertBlockedAsync(int blocker, int waiting)
    {
        Assert.NotEqual(blocker, waiting);
        await using var audit = fixture.Create();
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(5))
        {
            var count = await audit.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.dm_exec_requests WHERE session_id={waiting} AND blocking_session_id={blocker} AND wait_type LIKE N'LCK_M_%'").SingleAsync();
            if (count == 1) return;
            await Task.Delay(25);
        }
        Assert.Fail($"SQL session {waiting} was not observed blocked by {blocker}.");
    }

    [Theory]
    [InlineData(2,"Cancelled",5)]
    [InlineData(3,"Closed",4)]
    public async Task VisitAndTerminalTransitionSerializeAcrossIndependentSessions(byte source, string target, byte terminal)
    {
        var row = await VisitFixtures.CreateAsync(fixture, source, withVisit: source == 3);
        var visitGate = new Gate(); var statusGate = new Gate();
        Task<HttpResponseMessage>? visiting = null, changing = null;
        await using var baseFactory = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now);
        await using var visitFactory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IVisitRepository>();
            services.AddScoped<IVisitRepository>(sp => new GatedVisit(sp.GetRequiredService<PropertyMaintenanceDbContext>(), visitGate));
        }));
        await using var statusBase = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now.AddMinutes(1));
        await using var statusFactory = statusBase.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMaintenanceRequestRepository>();
            services.AddScoped<IMaintenanceRequestRepository>(sp => new ObservedRequests(sp.GetRequiredService<PropertyMaintenanceDbContext>(), statusGate));
        }));
        try
        {
            visiting = visitFactory.CreateClient().PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                VisitFixtures.Json($$"""{"technicianId":{{row.Other}},"notes":"T07 concurrent visit","cost":1.25}"""));
            await visitGate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
            changing = statusFactory.CreateClient().PatchAsync($"/api/maintenance-requests/{row.Id}/status",
                VisitFixtures.Json($$"""{"status":"{{target}}"}"""));
            await statusGate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await AssertBlockedAsync(visitGate.Spid, statusGate.Spid);
            visitGate.Continue.TrySetResult();
            Assert.Equal(HttpStatusCode.Created, (await visiting).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await changing).StatusCode);
            await using var audit = fixture.Create();
            var saved = await audit.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(terminal, saved.StatusId);
            Assert.Equal(source == 3 ? 2 : 1, await audit.Visits.CountAsync(v => v.RequestId == row.Id));
            Assert.False(await audit.Visits.AnyAsync(v => v.RequestId == row.Id && v.VisitedAt > saved.ClosedAt));
            var late = await baseFactory.CreateClient().PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                VisitFixtures.Json($$"""{"technicianId":{{row.Other}},"notes":"T07 forbidden late visit","cost":0}"""));
            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
            Assert.Equal("INVALID_VISIT_STATUS", (await late.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            Assert.Equal(source == 3 ? 2 : 1, await audit.Visits.CountAsync(v => v.RequestId == row.Id));
        }
        finally
        {
            visitGate.Continue.TrySetResult();
            if (visiting != null) try { await visiting; } catch { }
            if (changing != null) try { await changing; } catch { }
            await VisitFixtures.CleanAsync(fixture, row.Id);
        }
    }

    [Fact]
    public async Task TechnicianDeactivationWaitsUntilValidatedVisitCommits()
    {
        var row = await VisitFixtures.CreateAsync(fixture, 2);
        var gate = new Gate(); var writerGate = new Gate();
        int techId;
        await using (var setup = fixture.Create())
        {
            var tech = new Technician { Name = "T07 temporary concurrency technician", SpecialtyId = 3, IsActive = true };
            setup.Technicians.Add(tech); await setup.SaveChangesAsync(); techId = tech.TechnicianId;
        }
        Task<HttpResponseMessage>? visiting = null; Task? deactivating = null;
        await using var baseFactory = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IVisitRepository>();
            services.AddScoped<IVisitRepository>(sp => new GatedVisit(sp.GetRequiredService<PropertyMaintenanceDbContext>(), gate));
        }));
        try
        {
            visiting = factory.CreateClient().PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                VisitFixtures.Json($$"""{"technicianId":{{techId}},"notes":"T07 before deactivation","cost":0}"""));
            await gate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
            deactivating = Task.Run(async () =>
            {
                await using var writer = fixture.Create();
                await using var transaction = await writer.Database.BeginTransactionAsync();
                writerGate.Spid = await writer.Database.SqlQuery<int>($"SELECT CONVERT(int,@@SPID) AS Value").SingleAsync();
                writerGate.Ready.TrySetResult();
                await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Technicians SET IsActive=0 WHERE TechnicianId={techId}");
                await transaction.CommitAsync();
            });
            await writerGate.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await AssertBlockedAsync(gate.Spid, writerGate.Spid);
            gate.Continue.TrySetResult();
            Assert.Equal(HttpStatusCode.Created, (await visiting).StatusCode);
            await deactivating;
            await using var audit = fixture.Create();
            Assert.False(await audit.Technicians.Where(t => t.TechnicianId == techId).Select(t => t.IsActive).SingleAsync());
            Assert.Equal(1, await audit.Visits.CountAsync(v => v.RequestId == row.Id && v.TechnicianId == techId));
            var late = await baseFactory.CreateClient().PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                VisitFixtures.Json($$"""{"technicianId":{{techId}},"notes":"T07 after deactivation","cost":0}"""));
            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
            Assert.Equal("TECHNICIAN_INACTIVE", (await late.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            Assert.Equal(1, await audit.Visits.CountAsync(v => v.RequestId == row.Id));
        }
        finally
        {
            gate.Continue.TrySetResult();
            if (visiting != null) try { await visiting; } catch { }
            if (deactivating != null) try { await deactivating; } catch { }
            await VisitFixtures.CleanAsync(fixture, row.Id);
            await using var cleanup = fixture.Create();
            await cleanup.Technicians.Where(t => t.TechnicianId == techId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task FailureAfterVisitSaveRollsBackInsertion()
    {
        var row = await VisitFixtures.CreateAsync(fixture, 2);
        var gate = new Gate(); gate.Continue.TrySetResult();
        await using var baseFactory = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IVisitRepository>();
            services.AddScoped<IVisitRepository>(sp => new GatedVisit(sp.GetRequiredService<PropertyMaintenanceDbContext>(), gate, failAfterSave: true));
        }));
        try
        {
            var response = await factory.CreateClient().PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                VisitFixtures.Json($$"""{"technicianId":{{row.Other}},"notes":"T07 rollback fixture","cost":0}"""));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await using var audit = fixture.Create();
            Assert.False(await audit.Visits.AnyAsync(v => v.RequestId == row.Id));
            Assert.Equal(2, await audit.MaintenanceRequests.Where(r => r.RequestId == row.Id).Select(r => r.StatusId).SingleAsync());
        }
        finally { await VisitFixtures.CleanAsync(fixture, row.Id); }
    }
}
