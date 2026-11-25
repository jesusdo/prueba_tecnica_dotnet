using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.DTOs;

public sealed record MaintenanceRequestResponse(int RequestId, int UnitId, int TenantId, int? TechnicianId,
    string Category, RequestPriorityId Priority, RequestStatusId Status, string Description, DateTime CreatedAt, DateTime? ClosedAt)
{
    public static MaintenanceRequestResponse FromModel(MaintenanceRequestModel model) => new(
        model.RequestId, model.UnitId, model.TenantId, model.TechnicianId, model.Category,
        (RequestPriorityId)model.PriorityId, (RequestStatusId)model.StatusId, model.Description, model.CreatedAt, model.ClosedAt);
}
