using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Entities;
using PropertyMaintenance.Api.Mappings;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class MaintenanceRequestRepository(PropertyMaintenanceDbContext context) : IMaintenanceRequestRepository
{
    public async Task<PagedResult<RequestReadModel>> ListAsync(RequestFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.MaintenanceRequests.AsNoTracking();
        if (filter.StatusId.HasValue) query = query.Where(r => r.StatusId == filter.StatusId.Value);
        if (filter.PriorityId.HasValue) query = query.Where(r => r.PriorityId == filter.PriorityId.Value);
        if (filter.PropertyId.HasValue) query = query.Where(r => r.Unit.PropertyId == filter.PropertyId.Value);
        if (filter.FromUtc.HasValue) query = query.Where(r => r.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToExclusiveUtc.HasValue) query = query.Where(r => r.CreatedAt < filter.ToExclusiveUtc.Value);
        var count = await query.CountAsync(cancellationToken);
        // CountAsync fits INT; any larger offset must be an empty page. Avoid
        // narrowing the offset until it is known to fit Skip's INT argument.
        if (filter.Offset >= count) return new PagedResult<RequestReadModel>(filter.Page, filter.PageSize, count, []);
        var items = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.RequestId)
            .Skip((int)filter.Offset).Take(filter.PageSize)
            .Select(r => new RequestReadModel(r.RequestId, r.Unit.PropertyId, r.Unit.Property.Name, r.UnitId, r.Unit.Number,
                r.TenantId, r.Tenant.FullName, r.TechnicianId, r.Technician == null ? null : r.Technician.Name,
                r.Category, (RequestPriorityId)r.PriorityId, (RequestStatusId)r.StatusId, r.Description, r.CreatedAt, r.ClosedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<RequestReadModel>(filter.Page, filter.PageSize, count, items);
    }

    public async Task LockForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // Both status changes and visits lock the parent before dependent rows.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT RequestId FROM dbo.MaintenanceRequests WITH (UPDLOCK,HOLDLOCK) WHERE RequestId={id}", cancellationToken);
    }

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
