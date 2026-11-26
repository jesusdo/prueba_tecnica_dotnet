using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DTOs;
namespace PropertyMaintenance.Api.Controllers;
[ApiController, Route("api/tenants")]
public sealed class TenantsController(TenantService service) : ControllerBase
{
    [HttpGet("{id}/history")]
    [ProducesResponseType<TenantHistoryResponse>(200)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<TenantHistoryResponse>> GetHistory([Range(1,int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok((await service.GetHistoryAsync(id, cancellationToken)).ToResponse());
}
