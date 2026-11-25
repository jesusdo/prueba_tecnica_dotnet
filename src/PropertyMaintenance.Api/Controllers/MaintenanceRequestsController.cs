using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;

namespace PropertyMaintenance.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public sealed class MaintenanceRequestsController(MaintenanceRequestService service) : ControllerBase
{
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
