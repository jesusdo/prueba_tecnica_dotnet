using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.Mappings;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public sealed class VisitRepository(PropertyMaintenanceDbContext context) : IVisitRepository
{
    public async Task<VisitModel> AddAsync(VisitModel model, CancellationToken cancellationToken = default)
    {
        var entity = new Visit { RequestId = model.RequestId, TechnicianId = model.TechnicianId,
            VisitedAt = model.VisitedAt, Notes = model.Notes, Cost = model.Cost };
        context.Visits.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
