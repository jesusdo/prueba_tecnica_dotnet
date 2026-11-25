using Microsoft.AspNetCore.Mvc;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;

namespace PropertyMaintenance.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public sealed class MaintenanceRequestsController(MaintenanceRequestService service) : ControllerBase
{
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
