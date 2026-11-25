using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PropertyMaintenance.Api.Business.Enums;

namespace PropertyMaintenance.Api.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateMaintenanceRequestDto
{
    [Range(1, int.MaxValue)] public int UnitId { get; init; }
    [Range(1, int.MaxValue)] public int TenantId { get; init; }
    [Required, StringLength(100)] public string Category { get; init; } = "";
    [Required, EnumDataType(typeof(RequestPriorityId))] public RequestPriorityId? Priority { get; init; }
    [Required, StringLength(2000)] public string Description { get; init; } = "";
}
