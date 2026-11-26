using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Context;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public sealed class ReportRepository(PropertyMaintenanceDbContext context) : IReportRepository
{
    public async Task<IReadOnlyList<PropertySummaryModel>> GetPropertiesSummaryAsync(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var start = new SqlParameter("@From", SqlDbType.Date) { Value = from };
        var end = new SqlParameter("@To", SqlDbType.Date) { Value = to };
        var rows = await context.Database.SqlQuery<PropertySummaryRow>(
            $"EXEC dbo.usp_GetPropertiesSummary @From={start}, @To={end}").ToListAsync(cancellationToken);
        return rows.Select(r => new PropertySummaryModel(r.PropertyId, r.PropertyName, r.OpenRequests,
            r.ClosedRequests, r.AvgResolutionHours, r.TotalVisitCost)).ToArray();
    }

    public async Task<IReadOnlyList<TechnicianWorkloadModel>> GetTechniciansWorkloadAsync(DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var reference = new SqlParameter("@AsOfUtc", SqlDbType.DateTime2) { Scale = 3, Value = asOfUtc };
        var rows = await context.Database.SqlQuery<TechnicianWorkloadRow>(
            $"EXEC dbo.usp_GetTechniciansWorkload @AsOfUtc={reference}").ToListAsync(cancellationToken);
        return rows.Select(r => new TechnicianWorkloadModel(r.TechnicianId, r.TechnicianName, r.AssignedRequests,
            r.InProgressRequests, r.TotalWorkload, r.VisitsLastMonth, r.TotalVisitCost)).ToArray();
    }
}

// Unmapped SQL result types reflect the current manual procedures exactly.
// The integer average and nullable technician cost are known SQL debt, not corrected here.
internal sealed class PropertySummaryRow
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = null!;
    public int OpenRequests { get; set; }
    public int ClosedRequests { get; set; }
    public int? AvgResolutionHours { get; set; }
    [Column(TypeName = "decimal(38,2)")]
    public decimal TotalVisitCost { get; set; }
}

internal sealed class TechnicianWorkloadRow
{
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = null!;
    public int AssignedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int TotalWorkload { get; set; }
    public int VisitsLastMonth { get; set; }
    [Column(TypeName = "decimal(38,2)")]
    public decimal? TotalVisitCost { get; set; }
}
