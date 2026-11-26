using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.Api.Business.Models;

public sealed record RequestFilter
{
    public int Page { get; }
    public int PageSize { get; }
    public byte? StatusId { get; }
    public byte? PriorityId { get; }
    public int? PropertyId { get; }
    public DateTime? FromUtc { get; }
    public DateTime? ToExclusiveUtc { get; }
    public long Offset => ((long)Page - 1) * PageSize;

    private RequestFilter(int page, int pageSize, byte? status, byte? priority, int? property, DateTime? from, DateTime? to)
    { Page = page; PageSize = pageSize; StatusId = status; PriorityId = priority; PropertyId = property; FromUtc = from; ToExclusiveUtc = to; }

    public static RequestFilter Create(int page = 1, int pageSize = 20, string? status = null, string? priority = null,
        int? propertyId = null, DateOnly? from = null, DateOnly? to = null)
    {
        if (page <= 0 || pageSize is < 1 or > 100 || propertyId <= 0 || (from.HasValue && to.HasValue && from > to))
            throw new RequestException(400, "INVALID_INPUT", "Pagination, identifiers or date range are invalid.");
        return new RequestFilter(page, pageSize, Parse<RequestStatusId>(status), Parse<RequestPriorityId>(priority), propertyId,
            from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to.HasValue && to.Value < DateOnly.MaxValue ? to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null);
    }

    private static byte? Parse<T>(string? value) where T : struct, Enum
    {
        if (value is null) return null;
        // JSON enum converters do not apply to query-string model binding.
        var name = Enum.GetNames<T>().FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        if (name is null) throw new RequestException(400, "INVALID_INPUT", "Status and priority must be named catalog values.");
        return Convert.ToByte(Enum.Parse<T>(name));
    }
}
