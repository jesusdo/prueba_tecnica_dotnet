using PropertyMaintenance.Api.Business.Enums;

namespace PropertyMaintenance.Api.Business.Models;

public sealed class MaintenanceRequestModel
{
    public static bool CanTransition(RequestStatusId current, RequestStatusId target) => (current, target) switch
    {
        (RequestStatusId.Open, RequestStatusId.Assigned or RequestStatusId.Cancelled) => true,
        (RequestStatusId.Assigned, RequestStatusId.InProgress or RequestStatusId.Cancelled) => true,
        (RequestStatusId.InProgress, RequestStatusId.Closed) => true,
        _ => false
    };
    public int RequestId { get; set; }
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public int? TechnicianId { get; set; }
    public string Category { get; set; } = "";
    public byte PriorityId { get; set; }
    public byte StatusId { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
