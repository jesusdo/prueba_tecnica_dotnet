using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateVisitDto : IValidatableObject
{
    [Required, Range(1, int.MaxValue)] public int? TechnicianId { get; init; }
    [Required, StringLength(2000)] public string Notes { get; init; } = "";
    [Required, JsonConverter(typeof(VisitCostJsonConverter))] public decimal? Cost { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cost.HasValue && !VisitModel.IsValidCost(Cost.Value))
            yield return new ValidationResult("Cost must be nonnegative and fit decimal(18,2) without rounding.", [nameof(Cost)]);
    }
}
