using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.UnitTests;

public sealed class MaintenanceRequestServiceTests
{
    private sealed class Clock : TimeProvider
    {
        public int Calls;
        public override DateTimeOffset GetUtcNow()
        {
            Calls++;
            return new DateTimeOffset(2026, 11, 25, 23, 59, 59, TimeSpan.Zero).AddTicks(9999999);
        }
    }
    private sealed class Repositories : IUnitRepository, ITenantRepository, ILeaseRepository, IMaintenanceRequestRepository, ITechnicianRepository
    {
        public bool Unit = true, Tenant = true, Lease = true;
        public int Writes;
        public DateOnly Date;
        public MaintenanceRequestModel? Added;
        public Task<TenantHistoryReadModel?> GetHistoryAsync(int id, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<RequestReadModel>> ListAsync(RequestFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task LockForUpdateAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool?> GetActiveStatusAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<bool?>(true);
        public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task<bool> IUnitRepository.ExistsAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Unit);
        Task<bool> ITenantRepository.ExistsAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Tenant);
        public Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default)
        { Date = date; return Task.FromResult(Lease); }
        public Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Added);
        public Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
        { Writes++; Added = model; model.RequestId = 42; model.RowVersion = new byte[8]; return Task.FromResult(model); }
    }
    [Fact]
    public async Task ValidLeaseCreatesOpenUnassignedRequestUsingOneUtcInstant()
    {
        var data = new Repositories(); var clock = new Clock();
        var service = new MaintenanceRequestService(data, data, data, data, clock, data);
        var saved = await service.CreateAsync(1, 2, " Plumbing ", RequestPriorityId.High, " Leak ");
        Assert.Equal(1, data.Writes); Assert.Equal(42, saved.RequestId);
        Assert.Equal((byte)RequestStatusId.Open, saved.StatusId);
        Assert.Null(saved.TechnicianId); Assert.Null(saved.ClosedAt);
        Assert.Equal("Plumbing", saved.Category); Assert.Equal("Leak", saved.Description);
        Assert.Equal(new DateOnly(2026, 11, 25), data.Date);
        Assert.Equal(data.Date, DateOnly.FromDateTime(saved.CreatedAt));
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind); Assert.Equal(1, clock.Calls);
        Assert.Equal(0, saved.CreatedAt.Ticks % TimeSpan.TicksPerMillisecond);
    }
    [Theory]
    [InlineData(false, true, true, "UNIT_NOT_FOUND", 404)]
    [InlineData(true, false, true, "TENANT_NOT_FOUND", 404)]
    [InlineData(true, true, false, "ACTIVE_LEASE_REQUIRED", 409)]
    public async Task FailedRuleNeverPersists(bool unit, bool tenant, bool lease, string code, int status)
    {
        var data = new Repositories { Unit = unit, Tenant = tenant, Lease = lease };
        var service = new MaintenanceRequestService(data, data, data, data, new Clock(), data);
        var error = await Assert.ThrowsAsync<RequestException>(() => service.CreateAsync(1, 2, "Test", RequestPriorityId.Low, "Test"));
        Assert.Equal(code, error.Code); Assert.Equal(status, error.Status); Assert.Equal(0, data.Writes);
    }
    [Theory]
    [InlineData(0, 2, "Test", "Test", 1)]
    [InlineData(1, 0, "Test", "Test", 1)]
    [InlineData(1, 2, "   ", "Test", 1)]
    [InlineData(1, 2, "Test", "   ", 1)]
    [InlineData(1, 2, "Test", "Test", 0)]
    [InlineData(1, 2, "Test", "Test", 255)]
    public async Task InvalidBusinessInputNeverPersists(int unit, int tenant, string category, string description, byte priority)
    {
        var data = new Repositories();
        var error = await Assert.ThrowsAsync<RequestException>(() => new MaintenanceRequestService(data, data, data, data, new Clock(), data)
            .CreateAsync(unit, tenant, category, (RequestPriorityId)priority, description));
        Assert.Equal(400, error.Status); Assert.Equal(0, data.Writes);
    }
}
