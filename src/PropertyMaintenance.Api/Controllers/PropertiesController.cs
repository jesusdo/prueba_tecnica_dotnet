using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;
namespace PropertyMaintenance.Api.Controllers;
[ApiController, Route("api/properties")]
public sealed class PropertiesController(PropertyService service) : ControllerBase
{
    [HttpGet("{id}/units")]
    [ProducesResponseType<IReadOnlyList<UnitResponse>>(200)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<IReadOnlyList<UnitResponse>>> GetUnits([Range(1,int.MaxValue)] int id, CancellationToken cancellationToken)
    {
        var units = await service.GetUnitsAsync(id, cancellationToken);
        return Ok(units.Select(u => u.ToResponse()).ToArray());
    }
}
