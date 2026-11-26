using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;
using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public sealed class MaintenanceRequestsController(MaintenanceRequestService service, VisitService visits) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<RequestListItemResponse>>(200)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<PagedResult<RequestListItemResponse>>> List([FromQuery] RequestListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(query.Page, query.PageSize, query.Status, query.Priority, query.PropertyId,
            query.From, query.To, cancellationToken);
        return Ok(new PagedResult<RequestListItemResponse>(result.Page, result.PageSize, result.TotalCount,
            result.Items.Select(r => r.ToResponse()).ToArray()));
    }

    [HttpPost("{id}/visits")]
    [ProducesResponseType<VisitResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VisitResponse>> CreateVisit([Range(1, int.MaxValue)] int id, CreateVisitDto input,
        CancellationToken cancellationToken)
    {
        var saved = await visits.CreateAsync(id, input.TechnicianId!.Value, input.Notes, input.Cost!.Value, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, VisitResponse.FromModel(saved));
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType<MaintenanceRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaintenanceRequestResponse>> ChangeStatus([Range(1, int.MaxValue)] int id,
        ChangeMaintenanceRequestStatusDto input, CancellationToken cancellationToken)
    {
        var saved = await service.ChangeStatusAsync(id, input.Status!.Value, input.TechnicianId, cancellationToken);
        return Ok(MaintenanceRequestResponse.FromModel(saved));
    }

    [HttpPost]
    [ProducesResponseType<MaintenanceRequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaintenanceRequestResponse>> Create(CreateMaintenanceRequestDto input, CancellationToken cancellationToken)
    {
        var saved = await service.CreateAsync(input.UnitId, input.TenantId, input.Category,
            input.Priority!.Value, input.Description, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, MaintenanceRequestResponse.FromModel(saved));
    }
}
