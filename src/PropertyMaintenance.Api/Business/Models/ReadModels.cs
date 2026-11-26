using PropertyMaintenance.Api.Business.Enums;

namespace PropertyMaintenance.Api.Business.Models;

public sealed record CurrentTenantReadModel(int TenantId, string FullName);
public sealed record UnitReadModel(int UnitId, string Number, short Floor, string Type, CurrentTenantReadModel? CurrentTenant);
public sealed record RequestReadModel(int RequestId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    int TenantId, string TenantName, int? TechnicianId, string? TechnicianName, string Category,
    RequestPriorityId Priority, RequestStatusId Status, string Description, DateTime CreatedAt, DateTime? ClosedAt);
public sealed record ContractReadModel(int LeaseId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    DateOnly StartDate, DateOnly? EndDate, decimal MonthlyRent, bool IsActive);
public sealed record HistoryRequestReadModel(int RequestId, int PropertyId, string PropertyName, int UnitId, string UnitNumber,
    string Category, RequestStatusId Status, RequestPriorityId Priority, DateTime CreatedAt, DateTime? ClosedAt, decimal TotalVisitCost);
public sealed record TenantHistoryReadModel(int TenantId, string FullName, IReadOnlyList<ContractReadModel> Contracts,
    IReadOnlyList<HistoryRequestReadModel> MaintenanceRequests);
public sealed record PagedResult<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items)
{
    public int TotalPages => (int)((TotalCount + (long)PageSize - 1) / PageSize);
}
