using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;

namespace PropertyMaintenance.Api.Controllers;

[ApiController, Route("api/reports")]
public sealed class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet("properties-summary")]
    [ProducesResponseType<IReadOnlyList<PropertySummaryResponse>>(200)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<IReadOnlyList<PropertySummaryResponse>>> PropertiesSummary(
        [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to, CancellationToken cancellationToken)
    {
        var rows = await service.GetPropertiesSummaryAsync(from!.Value, to!.Value, cancellationToken);
        return Ok(rows.Select(r => new PropertySummaryResponse(r.PropertyId, r.PropertyName, r.OpenRequests,
            r.ClosedRequests, r.AvgResolutionHours, r.TotalVisitCost)).ToArray());
    }

    [HttpGet("technicians-workload")]
    [ProducesResponseType<IReadOnlyList<TechnicianWorkloadResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<IReadOnlyList<TechnicianWorkloadResponse>>> TechniciansWorkload(CancellationToken cancellationToken)
    {
        var rows = await service.GetTechniciansWorkloadAsync(cancellationToken);
        return Ok(rows.Select(r => new TechnicianWorkloadResponse(r.TechnicianId, r.TechnicianName, r.AssignedRequests,
            r.InProgressRequests, r.TotalWorkload, r.VisitsLastMonth, r.TotalVisitCost)).ToArray());
    }
}
