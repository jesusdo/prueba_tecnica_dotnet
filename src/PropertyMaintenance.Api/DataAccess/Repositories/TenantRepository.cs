using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Context;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public sealed class TenantRepository(PropertyMaintenanceDbContext context) : ITenantRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Tenants.AsNoTracking().AnyAsync(t => t.TenantId == id, cancellationToken);
}
