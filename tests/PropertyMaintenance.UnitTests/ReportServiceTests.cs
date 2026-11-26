using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.UnitTests;

public sealed class ReportServiceTests
{
    private sealed class Reports : IReportRepository
    {
        public int Calls;
        public DateTime Reference;
        public Task<IReadOnlyList<PropertySummaryModel>> GetPropertiesSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult<IReadOnlyList<PropertySummaryModel>>([]); }
        public Task<IReadOnlyList<TechnicianWorkloadModel>> GetTechniciansWorkloadAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
        { Calls++; Reference = asOfUtc; return Task.FromResult<IReadOnlyList<TechnicianWorkloadModel>>([]); }
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 11, 26, 1, 0, 0, TimeSpan.FromHours(2)).AddTicks(12345);
    }

    [Theory]
    [InlineData("2026-12-01", "2026-01-01")]
    [InlineData("9999-12-31", "9999-12-31")]
    public async Task InvalidRangesDoNotCallSql(string start, string end)
    {
        var reports = new Reports();
        var service = new ReportService(reports, new Clock());
        var error = await Assert.ThrowsAsync<RequestException>(() => service.GetPropertiesSummaryAsync(DateOnly.Parse(start), DateOnly.Parse(end)));
        Assert.Equal(400, error.Status); Assert.Equal("INVALID_INPUT", error.Code); Assert.Equal(0, reports.Calls);
    }

    [Fact]
    public async Task MinimumDateAndEmptyResultsAreSupported()
    {
        var reports = new Reports();
        var service = new ReportService(reports, new Clock());
        Assert.Empty(await service.GetPropertiesSummaryAsync(DateOnly.MinValue, DateOnly.MinValue));
        Assert.Empty(await service.GetTechniciansWorkloadAsync());
        Assert.Equal(2, reports.Calls);
        Assert.Equal(new DateTime(2026, 11, 25, 23, 0, 0, 1, DateTimeKind.Utc), reports.Reference);
        Assert.Equal(DateTimeKind.Utc, reports.Reference.Kind);
    }
}
