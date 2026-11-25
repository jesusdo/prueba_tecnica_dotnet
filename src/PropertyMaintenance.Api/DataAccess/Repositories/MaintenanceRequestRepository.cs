using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.Mappings;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class MaintenanceRequestRepository(PropertyMaintenanceDbContext context) : IMaintenanceRequestRepository
{
    public Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Visits.FromSqlInterpolated($"SELECT * FROM dbo.Visits WITH (HOLDLOCK) WHERE RequestId={id}")
            .AsNoTracking().AnyAsync(cancellationToken);

    public async Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default)
    {
        var entity = new MaintenanceRequest
        {
            RequestId = model.RequestId, UnitId = model.UnitId, TenantId = model.TenantId,
            Category = model.Category, PriorityId = model.PriorityId, Description = model.Description,
            CreatedAt = model.CreatedAt, StatusId = model.StatusId, TechnicianId = model.TechnicianId,
            ClosedAt = model.ClosedAt, RowVersion = model.RowVersion.ToArray()
        };
        var entry = context.Attach(entity);
        entry.Property(r => r.RowVersion).OriginalValue = model.RowVersion.ToArray();
        entry.Property(r => r.StatusId).IsModified = true;
        entry.Property(r => r.TechnicianId).IsModified = true;
        entry.Property(r => r.ClosedAt).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

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
