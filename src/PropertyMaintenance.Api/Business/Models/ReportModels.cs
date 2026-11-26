namespace PropertyMaintenance.Api.Business.Models;

public sealed record PropertySummaryModel(int PropertyId, string PropertyName, int OpenRequests,
    int ClosedRequests, decimal? AvgResolutionHours, decimal TotalVisitCost);

public sealed record TechnicianWorkloadModel(int TechnicianId, string TechnicianName, int AssignedRequests,
    int InProgressRequests, int TotalWorkload, int VisitsLastMonth, decimal? TotalVisitCost);
