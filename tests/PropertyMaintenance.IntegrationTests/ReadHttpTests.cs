using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyMaintenance.Api.DataAccess.Context;
using Xunit.Abstractions;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class ReadHttpTests(SqlFixture fixture, ITestOutputHelper output)
{
    [Theory]
    [InlineData("current")] [InlineData("start-boundary")] [InlineData("after-end")]
    public async Task UnitsPreserveVacanciesAndInclusiveLeases(string scenario)
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        var now=scenario=="start-boundary"?ReadFixtures.Now.AddDays(-10):scenario=="after-end"?ReadFixtures.Now.AddDays(1):ReadFixtures.Now;
        await using var factory=new RequestFactory(fixture.ConnectionString,now);
        var response=await factory.CreateClient().GetAsync($"/api/properties/{data.PropertyId}/units");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var units=(await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        Assert.Equal(5,units.Length);
        Assert.Equal(new[]{"U10","U2","U3","U4","U5"},units.Select(u=>u.GetProperty("number").GetString()));
        Assert.Equal(data.UnitIds,units.Select(u=>u.GetProperty("unitId").GetInt32()));
        Assert.Equal(scenario=="after-end"?JsonValueKind.Null:JsonValueKind.Object,units[0].GetProperty("currentTenant").ValueKind);
        if(scenario!="after-end") Assert.Equal(data.TenantId,units[0].GetProperty("currentTenant").GetProperty("tenantId").GetInt32());
        Assert.Equal(JsonValueKind.Null,units[1].GetProperty("currentTenant").ValueKind);
        Assert.Equal(scenario=="after-end"?JsonValueKind.Object:JsonValueKind.Null,units[2].GetProperty("currentTenant").ValueKind);
        Assert.Equal(data.OtherTenantId,units[3].GetProperty("currentTenant").GetProperty("tenantId").GetInt32());
        Assert.Equal(JsonValueKind.Null,units[4].GetProperty("currentTenant").ValueKind);
        Assert.All(units,u=>Assert.Contains(u.GetProperty("type").GetString(),new[]{"Apartment","Commercial"}));
    }

    [Fact]
    public async Task ExistingPropertyWithoutUnitsReturnsEmptyArray()
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var response=await factory.CreateClient().GetAsync($"/api/properties/{data.EmptyPropertyId}/units");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
    }

    [Theory]
    [InlineData("/api/properties/0/units",400)] [InlineData("/api/properties/abc/units",400)]
    [InlineData("/api/properties/2147483647/units",404)]
    [InlineData("/api/tenants/-1/history",400)] [InlineData("/api/tenants/abc/history",400)]
    [InlineData("/api/tenants/2147483647/history",404)]
    [InlineData("/api/maintenance-requests?page=0",400)] [InlineData("/api/maintenance-requests?page=abc",400)]
    [InlineData("/api/maintenance-requests?pageSize=101",400)] [InlineData("/api/maintenance-requests?pageSize=0",400)]
    [InlineData("/api/maintenance-requests?status=1",400)] [InlineData("/api/maintenance-requests?status=Unknown",400)]
    [InlineData("/api/maintenance-requests?status=",400)]
    [InlineData("/api/maintenance-requests?priority=2",400)] [InlineData("/api/maintenance-requests?priority=Unknown",400)]
    [InlineData("/api/maintenance-requests?propertyId=0",400)]
    [InlineData("/api/maintenance-requests?from=bad-date",400)]
    [InlineData("/api/maintenance-requests?to=10000-01-01",400)]
    [InlineData("/api/maintenance-requests?from=2026-11-21&to=2026-11-20",400)]
    public async Task InvalidReadsReturnProblemDetails(string url,int expected)
    {
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var response=await factory.CreateClient().GetAsync(url);
        Assert.Equal(expected,(int)response.StatusCode);
        Assert.Equal("application/problem+json",response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(expected,(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("all","")] [InlineData("status","&status=Open")] [InlineData("priority","&priority=High")]
    [InlineData("from","&from=2026-11-20")] [InlineData("to","&to=2026-11-20")]
    [InlineData("combined","&status=Open&priority=High&from=2026-11-20&to=2026-11-20")]
    [InlineData("extremes","&from=0001-01-01&to=9999-12-31")]
    [InlineData("no-maximum-day","&from=9999-12-31&to=9999-12-31")]
    [InlineData("minimum-day","&to=0001-01-01")]
    public async Task FiltersApplyInSqlBeforeCountAndPaging(string scenario,string filter)
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var response=await factory.CreateClient().GetAsync($"/api/maintenance-requests?propertyId={data.PropertyId}{filter}");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var page=await response.Content.ReadFromJsonAsync<JsonElement>();
        int[] expected=scenario switch
        {
            "status" or "from"=>[data.NextDayRequest,data.SecondTiedRequest,data.FirstTiedRequest],
            "priority"=>[data.NextDayRequest,data.FirstTiedRequest,data.HistoricalRequest],
            "to"=>[data.SecondTiedRequest,data.FirstTiedRequest,data.HistoricalRequest],
            "combined"=>[data.FirstTiedRequest], "no-maximum-day" or "minimum-day"=>[],
            _=>[data.NextDayRequest,data.SecondTiedRequest,data.FirstTiedRequest,data.HistoricalRequest]
        };
        var items=page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(expected,items.Select(i=>i.GetProperty("requestId").GetInt32()));
        Assert.Equal(expected.Length,page.GetProperty("totalCount").GetInt32());
        Assert.Equal(expected.Length==0?0:1,page.GetProperty("totalPages").GetInt32());
        foreach(var item in items)
        {
            Assert.Equal(data.PropertyId,item.GetProperty("propertyId").GetInt32()); Assert.Equal("T08 Property",item.GetProperty("propertyName").GetString());
            Assert.Equal(data.TenantId,item.GetProperty("tenantId").GetInt32()); Assert.Equal("T08 Tenant",item.GetProperty("tenantName").GetString());
            Assert.StartsWith("U",item.GetProperty("unitNumber").GetString());
            Assert.Equal(DateTimeKind.Utc,item.GetProperty("createdAt").GetDateTime().Kind);
            Assert.Equal(item.GetProperty("technicianId").ValueKind==JsonValueKind.Null, item.GetProperty("technicianName").ValueKind==JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task PagingDefaultsOverflowAndStableOrder()
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now); using var client=factory.CreateClient();
        var defaults=await client.GetFromJsonAsync<JsonElement>("/api/maintenance-requests");
        Assert.Equal(1,defaults.GetProperty("page").GetInt32()); Assert.Equal(20,defaults.GetProperty("pageSize").GetInt32());
        await using var connection=new SqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var command=connection.CreateCommand(); command.CommandText="SELECT COUNT(*) FROM dbo.MaintenanceRequests";
        Assert.Equal(Convert.ToInt32(await command.ExecuteScalarAsync()),defaults.GetProperty("totalCount").GetInt32());
        var one=await client.GetFromJsonAsync<JsonElement>($"/api/maintenance-requests?propertyId={data.PropertyId}&pageSize=2");
        var two=await client.GetFromJsonAsync<JsonElement>($"/api/maintenance-requests?propertyId={data.PropertyId}&pageSize=2&page=2");
        var first=one.GetProperty("items").EnumerateArray().Select(i=>i.GetProperty("requestId").GetInt32()).ToArray();
        var second=two.GetProperty("items").EnumerateArray().Select(i=>i.GetProperty("requestId").GetInt32()).ToArray();
        Assert.Equal(new[]{data.NextDayRequest,data.SecondTiedRequest},first); Assert.Equal(new[]{data.FirstTiedRequest,data.HistoricalRequest},second);
        Assert.Empty(first.Intersect(second)); Assert.Equal(4,one.GetProperty("totalCount").GetInt32()); Assert.Equal(2,one.GetProperty("totalPages").GetInt32());
        foreach(var url in new[]{ $"/api/maintenance-requests?propertyId={data.PropertyId}&pageSize=2&page=3",
            $"/api/maintenance-requests?propertyId={data.PropertyId}&pageSize=100&page=2147483647", "/api/maintenance-requests?propertyId=2147483647" })
        {
            var response=await client.GetAsync(url); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            var page=await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Empty(page.GetProperty("items").EnumerateArray());
            Assert.Equal(url.EndsWith("propertyId=2147483647")?0:4,page.GetProperty("totalCount").GetInt32());
        }
    }

    [Fact]
    public async Task HistoryIncludesAllPeriodsAndExactCostsComparedWithIndependentSql()
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var response=await factory.CreateClient().GetAsync($"/api/tenants/{data.TenantId}/history"); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var history=await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(data.TenantId,history.GetProperty("tenantId").GetInt32()); Assert.Equal("T08 Tenant",history.GetProperty("fullName").GetString());
        var contracts=history.GetProperty("contracts").EnumerateArray().ToArray(); Assert.Equal(3,contracts.Length);
        Assert.Equal(new[]{false,true,false},contracts.Select(l=>l.GetProperty("isActive").GetBoolean()));
        Assert.Equal(new[]{300m,100m,200m},contracts.Select(l=>l.GetProperty("monthlyRent").GetDecimal()));
        var requests=history.GetProperty("maintenanceRequests").EnumerateArray().ToArray();
        Assert.Equal(new[]{data.NextDayRequest,data.SecondTiedRequest,data.FirstTiedRequest,data.HistoricalRequest},requests.Select(r=>r.GetProperty("requestId").GetInt32()));
        await using var connection=new SqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText="SELECT r.RequestId,COALESCE((SELECT SUM(v.Cost) FROM dbo.Visits v WHERE v.RequestId=r.RequestId),0) FROM dbo.MaintenanceRequests r WHERE r.TenantId=@tenant ORDER BY r.CreatedAt DESC,r.RequestId DESC";
        command.Parameters.AddWithValue("@tenant",data.TenantId);
        await using var reader=await command.ExecuteReaderAsync(); var index=0;
        while(await reader.ReadAsync())
        { Assert.Equal(reader.GetInt32(0),requests[index].GetProperty("requestId").GetInt32()); Assert.Equal(reader.GetDecimal(1),requests[index].GetProperty("totalVisitCost").GetDecimal()); index++; }
        Assert.Equal(4,index); Assert.Equal(30.35m,requests.Last().GetProperty("totalVisitCost").GetDecimal());
        Assert.All(requests.Take(3),r=>Assert.Equal(0m,r.GetProperty("totalVisitCost").GetDecimal()));
        // The historical request belongs to a lease that has ended, yet remains visible.
        Assert.True(requests.Last().GetProperty("createdAt").GetDateTime()<contracts[1].GetProperty("startDate").GetDateTime());
    }

    [Fact]
    public async Task InclusiveUpperDayIncludesLastMillisecondButExcludesNextMidnight()
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var db=fixture.Create();
        var row=new PropertyMaintenance.Api.DataAccess.Entities.MaintenanceRequest
        { UnitId=data.UnitIds[0],TenantId=data.TenantId,Category="T08 Category",PriorityId=1,StatusId=1,
            Description="T08 last millisecond",CreatedAt=new DateTime(2026,11,20,23,59,59,999,DateTimeKind.Utc) };
        db.MaintenanceRequests.Add(row); await db.SaveChangesAsync();
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var page=await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/maintenance-requests?propertyId={data.PropertyId}&from=2026-11-20&to=2026-11-20");
        var actual=page.GetProperty("items").EnumerateArray().Select(r=>r.GetProperty("requestId").GetInt32()).ToArray();
        await using var connection=new SqlConnection(fixture.ConnectionString); await connection.OpenAsync();
        await using var command=connection.CreateCommand();
        command.CommandText="SELECT r.RequestId FROM dbo.MaintenanceRequests r JOIN dbo.Units u ON u.UnitId=r.UnitId WHERE u.PropertyId=@property AND r.CreatedAt>=@from AND r.CreatedAt<@to ORDER BY r.CreatedAt DESC,r.RequestId DESC";
        command.Parameters.AddWithValue("@property",data.PropertyId);
        command.Parameters.Add(new SqlParameter("@from",System.Data.SqlDbType.DateTime2){Value=new DateTime(2026,11,20)});
        command.Parameters.Add(new SqlParameter("@to",System.Data.SqlDbType.DateTime2){Value=new DateTime(2026,11,21)});
        await using var reader=await command.ExecuteReaderAsync(); var expected=new List<int>();
        while(await reader.ReadAsync()) expected.Add(reader.GetInt32(0));
        Assert.Equal(expected,actual); Assert.Equal(3,page.GetProperty("totalCount").GetInt32());
        Assert.Contains(row.RequestId,actual); Assert.DoesNotContain(data.NextDayRequest,actual);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TenantWithoutRequestsReturnsEmptyCollectionsWhenAppropriate(bool noContracts)
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        await using var factory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        var id=noContracts?data.EmptyTenantId:data.OtherTenantId;
        var history=await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/tenants/{id}/history");
        Assert.Empty(history.GetProperty("maintenanceRequests").EnumerateArray());
        Assert.Equal(noContracts?0:1,history.GetProperty("contracts").GetArrayLength());
        if(!noContracts) Assert.True(history.GetProperty("contracts")[0].GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task SqlProjectionsUseBoundedQueriesAndServerSideOperations()
    {
        await using var data=await ReadFixtures.CreateAsync(fixture);
        var capture=new SqlCapture();
        await using var baseFactory=new RequestFactory(fixture.ConnectionString,ReadFixtures.Now);
        await using var factory=baseFactory.WithWebHostBuilder(builder=>builder.ConfigureServices(services=>
            services.AddDbContext<PropertyMaintenanceDbContext>(options=>options.AddInterceptors(capture))));
        using var client=factory.CreateClient();
        var response=await client.GetAsync($"/api/properties/{data.PropertyId}/units"); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal(2,capture.Commands.Count); var unitsSql=capture.Commands.Last();
        output.WriteLine("Units projection SQL:\n"+unitsSql);
        Assert.Contains("LEFT JOIN",unitsSql); Assert.Contains("StartDate",unitsSql); Assert.DoesNotContain("[Email]",unitsSql);
        capture.Commands.Clear();
        response=await client.GetAsync($"/api/maintenance-requests?propertyId={data.PropertyId}&status=Open&from=2026-11-20&to=2026-11-21&pageSize=1");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode); Assert.Equal(2,capture.Commands.Count);
        var pageSql=capture.Commands.Last(); Assert.Contains("OFFSET",pageSql); Assert.Contains("FETCH NEXT",pageSql);
        output.WriteLine("Requests count SQL:\n"+capture.Commands.First());
        output.WriteLine("Requests page SQL:\n"+pageSql);
        Assert.Contains("[CreatedAt] DESC",pageSql); Assert.Contains("[RequestId] DESC",pageSql); Assert.Contains("LEFT JOIN",pageSql);
        Assert.Contains("[CreatedAt] >=",pageSql); Assert.Contains("[CreatedAt] <",pageSql); Assert.DoesNotContain("[RowVersion]",pageSql);
        capture.Commands.Clear();
        response=await client.GetAsync($"/api/tenants/{data.TenantId}/history"); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal(3,capture.Commands.Count); var historySql=capture.Commands.Last();
        output.WriteLine("Tenant contracts SQL:\n"+capture.Commands[1]);
        output.WriteLine("Tenant request costs SQL:\n"+historySql);
        Assert.Contains("SUM(",historySql); Assert.Contains("COALESCE",historySql); Assert.DoesNotContain("[Leases]",historySql);
        Assert.DoesNotContain("[RowVersion]",historySql);
    }
}
