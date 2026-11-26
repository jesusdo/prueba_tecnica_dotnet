using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.Api.Business.Services;

public sealed class ReportService(IReportRepository reports, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<PropertySummaryModel>> GetPropertiesSummaryAsync(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        // The canonical procedure adds one day to @To. Reject its unrepresentable boundary.
        if (from > to || to == DateOnly.MaxValue)
            throw new RequestException(400, "INVALID_INPUT", "The date range is invalid; to must be earlier than 9999-12-31.");
        return reports.GetPropertiesSummaryAsync(from, to, cancellationToken);
    }

    public Task<IReadOnlyList<TechnicianWorkloadModel>> GetTechniciansWorkloadAsync(CancellationToken cancellationToken = default)
    {
        var utc = timeProvider.GetUtcNow().UtcDateTime;
        utc = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerMillisecond));
        return reports.GetTechniciansWorkloadAsync(utc, cancellationToken);
    }
}
