using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.DTOs;

public sealed record CurrentTenantResponse(int TenantId, string FullName);
public sealed record UnitResponse(int UnitId, string Number, short Floor, string Type, CurrentTenantResponse? CurrentTenant);
public sealed record RequestListItemResponse(int RequestId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    int TenantId, string TenantName, int? TechnicianId, string? TechnicianName, string Category,
    RequestPriorityId Priority, RequestStatusId Status, string Description, DateTime CreatedAt, DateTime? ClosedAt);
public sealed record ContractResponse(int LeaseId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    DateOnly StartDate, DateOnly? EndDate, decimal MonthlyRent, bool IsActive);
public sealed record HistoryRequestResponse(int RequestId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    string Category, RequestStatusId Status, RequestPriorityId Priority, DateTime CreatedAt, DateTime? ClosedAt, decimal TotalVisitCost);
public sealed record TenantHistoryResponse(int TenantId, string FullName, IReadOnlyList<ContractResponse> Contracts,
    IReadOnlyList<HistoryRequestResponse> MaintenanceRequests);

public static class ReadResponseMapping
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static UnitResponse ToResponse(this UnitReadModel item) => new(item.UnitId, item.Number, item.Floor, item.Type,
        item.CurrentTenant is { } t ? new CurrentTenantResponse(t.TenantId, t.FullName) : null);
    public static RequestListItemResponse ToResponse(this RequestReadModel r) => new(r.RequestId, r.PropertyId, r.PropertyName,
        r.UnitId, r.UnitNumber, r.TenantId, r.TenantName, r.TechnicianId, r.TechnicianName, r.Category, r.Priority,
        r.Status, r.Description, Utc(r.CreatedAt), r.ClosedAt is { } closed ? Utc(closed) : null);
    public static TenantHistoryResponse ToResponse(this TenantHistoryReadModel t) => new(t.TenantId, t.FullName,
        t.Contracts.Select(l => new ContractResponse(l.LeaseId, l.PropertyId, l.PropertyName, l.UnitId, l.UnitNumber,
            l.StartDate, l.EndDate, l.MonthlyRent, l.IsActive)).ToArray(),
        t.MaintenanceRequests.Select(r => new HistoryRequestResponse(r.RequestId, r.PropertyId, r.PropertyName, r.UnitId,
            r.UnitNumber, r.Category, r.Status, r.Priority, Utc(r.CreatedAt), r.ClosedAt is { } closed ? Utc(closed) : null,
            r.TotalVisitCost)).ToArray());
}
