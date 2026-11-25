using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.Api.Mappings;

public static class MaintenanceRequestMapping
{
    public static MaintenanceRequestModel ToModel(this MaintenanceRequest entity) => new()
    {
        RequestId = entity.RequestId, UnitId = entity.UnitId, TenantId = entity.TenantId,
        TechnicianId = entity.TechnicianId, Category = entity.Category,
        PriorityId = entity.PriorityId, StatusId = entity.StatusId, Description = entity.Description,
        CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc),
        ClosedAt = entity.ClosedAt is { } closed ? DateTime.SpecifyKind(closed, DateTimeKind.Utc) : null,
        RowVersion = entity.RowVersion.ToArray()
    };
}
