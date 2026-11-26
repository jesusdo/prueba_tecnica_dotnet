using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
namespace PropertyMaintenance.Api.DTOs;
public sealed class RequestListQuery
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1,100)] public int PageSize { get; init; } = 20;
    [Description("Open, Assigned, InProgress, Closed or Cancelled. Numeric values are rejected.")]
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string? Status { get; init; }
    [Description("Low, Medium, High or Urgent. Numeric values are rejected.")]
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string? Priority { get; init; }
    [Range(1, int.MaxValue)] public int? PropertyId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}
