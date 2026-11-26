using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.DataAccess.Context;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public sealed class TenantRepository(PropertyMaintenanceDbContext context) : ITenantRepository
{
    public async Task<TenantHistoryReadModel?> GetHistoryAsync(int id, DateOnly today, CancellationToken cancellationToken = default)
    {
        var tenant = await context.Tenants.AsNoTracking().Where(t => t.TenantId == id)
            .Select(t => new { t.TenantId, t.FullName }).SingleOrDefaultAsync(cancellationToken);
        if (tenant is null) return null;
        var contracts = await context.Leases.AsNoTracking().Where(l => l.TenantId == id)
            .OrderByDescending(l => l.StartDate).ThenByDescending(l => l.LeaseId)
            .Select(l => new ContractReadModel(l.LeaseId, l.Unit.PropertyId, l.Unit.Property.Name, l.UnitId, l.Unit.Number,
                l.StartDate, l.EndDate, l.MonthlyRent, l.StartDate <= today && (l.EndDate == null || l.EndDate >= today)))
            .ToListAsync(cancellationToken);
        var requests = await context.MaintenanceRequests.AsNoTracking().Where(r => r.TenantId == id)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.RequestId)
            .Select(r => new HistoryRequestReadModel(r.RequestId, r.Unit.PropertyId, r.Unit.Property.Name, r.UnitId, r.Unit.Number,
                r.Category, (RequestStatusId)r.StatusId, (RequestPriorityId)r.PriorityId, r.CreatedAt, r.ClosedAt,
                r.Visits.Sum(v => (decimal?)v.Cost) ?? 0m))
            .ToListAsync(cancellationToken);
        return new TenantHistoryReadModel(tenant.TenantId, tenant.FullName, contracts, requests);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Tenants.AsNoTracking().AnyAsync(t => t.TenantId == id, cancellationToken);
}
