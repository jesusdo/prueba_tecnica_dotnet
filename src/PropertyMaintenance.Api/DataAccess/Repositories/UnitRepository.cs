using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Context;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public sealed class UnitRepository(PropertyMaintenanceDbContext context) : IUnitRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Units.AsNoTracking().AnyAsync(u => u.UnitId == id, cancellationToken);
}
