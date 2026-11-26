using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.Business.Services;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.UnitTests;

public sealed class ReadQueryTests
{
    [Theory]
    [InlineData(0,20,null,null,null)] [InlineData(-1,20,null,null,null)]
    [InlineData(1,0,null,null,null)] [InlineData(1,101,null,null,null)]
    [InlineData(1,20,"1",null,null)] [InlineData(1,20,"Unknown",null,null)]
    [InlineData(1,20,"",null,null)] [InlineData(1,20,null,"2",null)]
    [InlineData(1,20,null,"Unknown",null)] [InlineData(1,20,null,null,0)]
    public void InvalidFiltersReturn400(int page, int size, string? status, string? priority, int? property)
    {
        var error = Assert.Throws<RequestException>(() => RequestFilter.Create(page,size,status,priority,property));
        Assert.Equal(400,error.Status); Assert.Equal("INVALID_INPUT",error.Code);
    }
    [Fact]
    public void DateBoundariesAreUtcAndCannotOverflow()
    {
        var minimum = RequestFilter.Create(from: DateOnly.MinValue, to: DateOnly.MinValue);
        Assert.Equal(DateTimeKind.Utc, minimum.FromUtc!.Value.Kind);
        Assert.Equal(new DateTime(1,1,2,0,0,0,DateTimeKind.Utc),minimum.ToExclusiveUtc);
        var maximum = RequestFilter.Create(from: DateOnly.MaxValue, to: DateOnly.MaxValue);
        Assert.Null(maximum.ToExclusiveUtc);
        Assert.Equal(new DateTime(9999,12,31,0,0,0,DateTimeKind.Utc), maximum.FromUtc);
        Assert.Throws<RequestException>(() => RequestFilter.Create(from: DateOnly.MaxValue,to: DateOnly.MinValue));
    }
    [Fact]
    public void NamedFiltersAndExtremeOffsetAreSafe()
    {
        var filter = RequestFilter.Create(int.MaxValue,100,"open","HIGH",3);
        Assert.Equal(214748364600L,filter.Offset); Assert.Equal((byte)1,filter.StatusId); Assert.Equal((byte)3,filter.PriorityId);
        Assert.Null(filter.FromUtc); Assert.Null(filter.ToExclusiveUtc);
        Assert.NotNull(RequestFilter.Create(to: new DateOnly(2026,11,25)).ToExclusiveUtc);
        Assert.Null(RequestFilter.Create(from: new DateOnly(2026,11,25)).ToExclusiveUtc);
    }
    [Theory]
    [InlineData(0,20,0)] [InlineData(1,20,1)] [InlineData(20,20,1)] [InlineData(21,20,2)]
    [InlineData(int.MaxValue,100,21474837)]
    public void TotalPagesUsesOverflowSafeIntegerArithmetic(int count, int size, int expected) =>
        Assert.Equal(expected,new PagedResult<int>(1,size,count,[]).TotalPages);

    private sealed class Data : IPropertyRepository, ITenantRepository
    {
        public bool Exists = true;
        public DateOnly? Date;
        public int Calls;
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(Exists); }
        public Task<IReadOnlyList<UnitReadModel>> GetUnitsAsync(int id, DateOnly today, CancellationToken cancellationToken = default)
        { Date = today; Calls++; return Task.FromResult<IReadOnlyList<UnitReadModel>>([]); }
        public Task<TenantHistoryReadModel?> GetHistoryAsync(int id, DateOnly today, CancellationToken cancellationToken = default)
        { Date = today; Calls++; return Task.FromResult<TenantHistoryReadModel?>(Exists ? new(id,"Fixture",[],[]) : null); }
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026,11,25,1,0,0,TimeSpan.FromHours(2));
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public async Task InvalidIdentifiersDoNotQuery(int id)
    {
        var data = new Data();
        await Assert.ThrowsAsync<RequestException>(() => new PropertyService(data,new Clock()).GetUnitsAsync(id));
        await Assert.ThrowsAsync<RequestException>(() => new TenantService(data,new Clock()).GetHistoryAsync(id));
        Assert.Equal(0,data.Calls);
    }
    [Fact]
    public async Task EmptyResultsAndUtcDateArePreserved()
    {
        var data = new Data();
        Assert.Empty(await new PropertyService(data,new Clock()).GetUnitsAsync(1));
        Assert.Equal(new DateOnly(2026,11,24),data.Date);
        var history = await new TenantService(data,new Clock()).GetHistoryAsync(1);
        Assert.Empty(history.Contracts); Assert.Empty(history.MaintenanceRequests);
        data.Exists = false;
        Assert.Equal("PROPERTY_NOT_FOUND",(await Assert.ThrowsAsync<RequestException>(() => new PropertyService(data,new Clock()).GetUnitsAsync(1))).Code);
        Assert.Equal("TENANT_NOT_FOUND",(await Assert.ThrowsAsync<RequestException>(() => new TenantService(data,new Clock()).GetHistoryAsync(1))).Code);
    }
}
