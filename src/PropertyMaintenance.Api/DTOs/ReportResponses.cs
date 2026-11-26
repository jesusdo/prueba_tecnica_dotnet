namespace PropertyMaintenance.Api.DTOs;

public sealed record PropertySummaryResponse(int PropertyId, string PropertyName, int OpenRequests,
    int ClosedRequests, decimal? AvgResolutionHours, decimal TotalVisitCost);

public sealed record TechnicianWorkloadResponse(int TechnicianId, string TechnicianName, int AssignedRequests,
    int InProgressRequests, int TotalWorkload, int VisitsLastMonth, decimal? TotalVisitCost);
