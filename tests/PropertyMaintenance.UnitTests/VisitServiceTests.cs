using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.UnitTests;

public sealed class VisitServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 18, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now.AddTicks(12345); }
    private sealed class Data : IMaintenanceRequestRepository, ITechnicianRepository, IVisitRepository
    {
        public MaintenanceRequestModel? Request = new() { RequestId = 7, StatusId = 2, TechnicianId = 1, CreatedAt = Now.AddDays(-1).UtcDateTime };
        public bool? Active = true;
        public int Writes;
        public bool Locked;
        public Task LockForUpdateAsync(int id, CancellationToken cancellationToken = default) { Locked = true; return Task.CompletedTask; }
        public Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        { Assert.True(Locked); return Task.FromResult(Request); }
        public Task<bool?> GetActiveStatusAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<VisitModel> AddAsync(VisitModel model, CancellationToken cancellationToken = default)
        { Writes++; model.VisitId = 42; return Task.FromResult(model); }
        public Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public VisitService Service() => new(this, this, this, new Clock());
    }
    [Theory]
    [InlineData(2, "0")]
    [InlineData(3, "12.34")]
    [InlineData(2, "9999999999999999.99")]
    public async Task AllowedStateSavesExactMoneyAndUtcWithoutChangingRequest(byte status, string amount)
    {
        var data = new Data(); data.Request!.StatusId = status;
        var cost = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var saved = await data.Service().CreateAsync(7, 9, "  Diagnostic  ", cost);
        Assert.Equal(42, saved.VisitId); Assert.Equal(7, saved.RequestId); Assert.Equal(9, saved.TechnicianId);
        Assert.Equal("Diagnostic", saved.Notes); Assert.Equal(cost, saved.Cost); Assert.Equal(1, data.Writes);
        Assert.Equal(Now.UtcDateTime.AddMilliseconds(1), saved.VisitedAt); Assert.Equal(DateTimeKind.Utc, saved.VisitedAt.Kind);
        Assert.Equal(status, data.Request.StatusId); Assert.Equal(1, data.Request.TechnicianId);
    }
    [Theory]
    [InlineData("open", 409, "INVALID_VISIT_STATUS")]
    [InlineData("closed", 409, "INVALID_VISIT_STATUS")]
    [InlineData("cancelled", 409, "INVALID_VISIT_STATUS")]
    [InlineData("missing", 404, "REQUEST_NOT_FOUND")]
    [InlineData("missing-tech", 404, "TECHNICIAN_NOT_FOUND")]
    [InlineData("inactive", 409, "TECHNICIAN_INACTIVE")]
    [InlineData("negative", 400, "INVALID_INPUT")]
    [InlineData("scale", 400, "INVALID_INPUT")]
    [InlineData("precision", 400, "INVALID_INPUT")]
    [InlineData("notes", 400, "INVALID_INPUT")]
    [InlineData("long-notes", 400, "INVALID_INPUT")]
    [InlineData("request-id", 400, "INVALID_INPUT")]
    [InlineData("tech-id", 400, "INVALID_INPUT")]
    [InlineData("chronology", 409, "INVALID_INPUT")]
    public async Task InvalidRuleDoesNotPersist(string scenario, int status, string code)
    {
        var data = new Data();
        data.Request!.StatusId = scenario switch { "open" => 1, "closed" => 4, "cancelled" => 5, _ => 2 };
        if (scenario == "missing") data.Request = null;
        if (scenario == "missing-tech") data.Active = null;
        if (scenario == "inactive") data.Active = false;
        if (scenario == "chronology") data.Request!.CreatedAt = Now.AddDays(1).UtcDateTime;
        var cost = scenario switch { "negative" => -1m, "scale" => 0.001m, "precision" => 10000000000000000m, _ => 1.23m };
        var notes = scenario switch { "notes" => "   ", "long-notes" => new string('x', 2001), _ => "Valid notes" };
        var error = await Assert.ThrowsAsync<RequestException>(() => data.Service().CreateAsync(scenario == "request-id" ? 0 : 7,
            scenario == "tech-id" ? 0 : 9, notes, cost));
        Assert.Equal(status, error.Status); Assert.Equal(code, error.Code); Assert.Equal(0, data.Writes);
    }
}
