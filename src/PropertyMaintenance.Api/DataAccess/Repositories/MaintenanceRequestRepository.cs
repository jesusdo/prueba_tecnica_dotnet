using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.Mappings;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class MaintenanceRequestRepository(PropertyMaintenanceDbContext context) : IMaintenanceRequestRepository
{
    public async Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.MaintenanceRequests.AsNoTracking().SingleOrDefaultAsync(r => r.RequestId == id, cancellationToken);
        return entity?.ToModel();
    }

    public async Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
    {
        var entity = new MaintenanceRequest
        {
            UnitId = model.UnitId, TenantId = model.TenantId, Category = model.Category,
            PriorityId = model.PriorityId, Description = model.Description,
            CreatedAt = model.CreatedAt
        };
        context.MaintenanceRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
