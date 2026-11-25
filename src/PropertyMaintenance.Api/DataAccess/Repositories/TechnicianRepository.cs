using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Context;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class TechnicianRepository(PropertyMaintenanceDbContext context) : ITechnicianRepository
{
    public Task<bool?> GetActiveStatusAsync(int id, CancellationToken cancellationToken = default) =>
        context.Technicians.FromSqlInterpolated($"SELECT * FROM dbo.Technicians WITH (HOLDLOCK) WHERE TechnicianId={id}")
            .AsNoTracking().Select(t => (bool?)t.IsActive).SingleOrDefaultAsync(cancellationToken);
}
