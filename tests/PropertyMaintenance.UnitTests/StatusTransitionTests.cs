using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.UnitTests;

public sealed class StatusTransitionTests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Instant.AddTicks(12345); }
    private sealed class Data : IUnitRepository, ITenantRepository, ILeaseRepository, IMaintenanceRequestRepository, ITechnicianRepository
    {
        public MaintenanceRequestModel? Model;
        public bool? Active = true;
        public bool Visits = true;
        public int Writes;
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool?> GetActiveStatusAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Visits);
        public Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Model);
        public Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
        { Writes++; return Task.FromResult(model); }
        public MaintenanceRequestService Service() => new(this, this, this, this, new Clock(), this);
    }
    public static IEnumerable<object[]> Combinations()
    {
        for (byte current = 1; current <= 5; current++)
            for (byte target = 1; target <= 5; target++)
                yield return [current, target, (current, target) is (1,2) or (1,5) or (2,3) or (2,5) or (3,4)];
    }
    private static Data Fixture(byte status) => new()
    {
        Model = new MaintenanceRequestModel
        {
            RequestId = 1, StatusId = status, TechnicianId = status == 1 ? null : 9,
            CreatedAt = Instant.AddDays(-1).UtcDateTime, RowVersion = new byte[8]
        }
    };

    [Theory, MemberData(nameof(Combinations))]
    public async Task AllTwentyFiveStatePairs(byte current, byte target, bool allowed)
    {
        var data = Fixture(current);
        Assert.Equal(allowed, MaintenanceRequestModel.CanTransition((RequestStatusId)current, (RequestStatusId)target));
        if (!allowed)
        {
            var error = await Assert.ThrowsAsync<RequestException>(() => data.Service().ChangeStatusAsync(1, (RequestStatusId)target, target == 2 ? 9 : null));
            Assert.Equal("INVALID_STATUS_TRANSITION", error.Code); Assert.Equal(0, data.Writes);
            Assert.Equal(current, data.Model!.StatusId);
            return;
        }
        var saved = await data.Service().ChangeStatusAsync(1, (RequestStatusId)target, target == 2 ? 9 : null);
        Assert.Equal(target, saved.StatusId); Assert.Equal(1, data.Writes);
        Assert.Equal(current == 1 && target == 5 ? null : (int?)9, saved.TechnicianId);
        if (target is 4 or 5)
        {
            Assert.NotNull(saved.ClosedAt); Assert.Equal(DateTimeKind.Utc, saved.ClosedAt.Value.Kind);
            Assert.Equal(0, saved.ClosedAt.Value.Ticks % TimeSpan.TicksPerMillisecond);
            Assert.True(saved.ClosedAt >= saved.CreatedAt);
        }
        else Assert.Null(saved.ClosedAt);
    }
    [Fact]
    public void ExactlyFivePairsAreAllowed() => Assert.Equal(5, Combinations().Count(pair => (bool)pair[2]));

    [Theory]
    [InlineData("missing-request", "REQUEST_NOT_FOUND", 404)]
    [InlineData("missing-technician", "TECHNICIAN_NOT_FOUND", 404)]
    [InlineData("inactive-technician", "TECHNICIAN_INACTIVE", 409)]
    [InlineData("no-visits", "VISIT_REQUIRED", 409)]
    [InlineData("missing-id", "INVALID_INPUT", 400)]
    [InlineData("reassign", "INVALID_INPUT", 400)]
    [InlineData("invalid-id", "INVALID_INPUT", 400)]
    [InlineData("invalid-status", "INVALID_INPUT", 400)]
    [InlineData("invalid-technician-id", "INVALID_INPUT", 400)]
    [InlineData("future-creation", "INVALID_STATUS_TRANSITION", 409)]
    public async Task FailureNeverSaves(string scenario, string code, int status)
    {
        var data = Fixture(scenario == "no-visits" ? (byte)3 : (byte)1);
        if (scenario == "missing-request") data.Model = null;
        if (scenario == "missing-technician") data.Active = null;
        if (scenario == "inactive-technician") data.Active = false;
        if (scenario == "no-visits") data.Visits = false;
        if (scenario == "future-creation") data.Model!.CreatedAt = Instant.AddDays(1).UtcDateTime;
        var target = scenario switch { "no-visits" => RequestStatusId.Closed, "reassign" or "future-creation" => RequestStatusId.Cancelled, "invalid-status" => (RequestStatusId)99, _ => RequestStatusId.Assigned };
        int? technician = scenario is "missing-id" or "no-visits" or "future-creation" or "invalid-status" ? null : scenario == "invalid-technician-id" ? 0 : 9;
        var error = await Assert.ThrowsAsync<RequestException>(() => data.Service().ChangeStatusAsync(scenario == "invalid-id" ? 0 : 1, target, technician));
        Assert.Equal(code, error.Code); Assert.Equal(status, error.Status); Assert.Equal(0, data.Writes);
    }
    [Fact]
    public async Task ExistingTechnicianActivityIsNotRevalidatedWhenStartingWork()
    {
        var data = Fixture(2); data.Active = false;
        var saved = await data.Service().ChangeStatusAsync(1, RequestStatusId.InProgress, null);
        Assert.Equal(9, saved.TechnicianId); Assert.Equal(3, saved.StatusId);
    }
}
