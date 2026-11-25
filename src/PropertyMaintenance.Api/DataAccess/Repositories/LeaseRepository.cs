using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Context;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class LeaseRepository(PropertyMaintenanceDbContext context) : ILeaseRepository
{
    public Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default) =>
        context.Leases.AsNoTracking().AnyAsync(lease => lease.UnitId == unitId && lease.TenantId == tenantId
            && lease.StartDate <= date && (lease.EndDate == null || lease.EndDate >= date), cancellationToken);
}
