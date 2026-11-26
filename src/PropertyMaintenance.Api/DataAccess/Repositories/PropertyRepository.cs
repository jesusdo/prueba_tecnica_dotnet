using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public sealed class PropertyRepository(PropertyMaintenanceDbContext context) : IPropertyRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Properties.AsNoTracking().AnyAsync(p => p.PropertyId == id, cancellationToken);
    public async Task<IReadOnlyList<UnitReadModel>> GetUnitsAsync(int id, DateOnly today, CancellationToken cancellationToken = default) =>
        await context.Units.AsNoTracking().Where(u => u.PropertyId == id).OrderBy(u => u.Number).ThenBy(u => u.UnitId)
            .Select(u => new UnitReadModel(u.UnitId, u.Number, u.Floor, u.UnitType.Code,
                u.Leases.Where(l => l.StartDate <= today && (l.EndDate == null || l.EndDate >= today))
                    .OrderByDescending(l => l.StartDate).ThenByDescending(l => l.LeaseId)
                    .Select(l => new CurrentTenantReadModel(l.TenantId, l.Tenant.FullName)).FirstOrDefault()))
            .ToListAsync(cancellationToken);
}
