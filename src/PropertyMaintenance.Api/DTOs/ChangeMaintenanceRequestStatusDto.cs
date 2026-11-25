using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PropertyMaintenance.Api.Business.Enums;

namespace PropertyMaintenance.Api.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangeMaintenanceRequestStatusDto : IValidatableObject
{
    [Required, EnumDataType(typeof(RequestStatusId))]
    public RequestStatusId? Status { get; init; }
    [Range(1, int.MaxValue)] public int? TechnicianId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == RequestStatusId.Assigned && TechnicianId is null)
            yield return new ValidationResult("TechnicianId is required for Assigned.", [nameof(TechnicianId)]);
        if (Status != RequestStatusId.Assigned && TechnicianId is not null)
            yield return new ValidationResult("TechnicianId is only allowed for Assigned.", [nameof(TechnicianId)]);
    }
}
