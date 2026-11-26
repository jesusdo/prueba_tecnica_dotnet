using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public interface IReportRepository
{
    Task<IReadOnlyList<PropertySummaryModel>> GetPropertiesSummaryAsync(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicianWorkloadModel>> GetTechniciansWorkloadAsync(DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
