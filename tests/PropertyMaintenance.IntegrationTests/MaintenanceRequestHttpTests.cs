using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyMaintenance.Api.DataAccess.Context;
using PropertyMaintenance.Api.DataAccess.Repositories;

namespace PropertyMaintenance.IntegrationTests;

public sealed class HttpClock(DateTimeOffset instant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instant;
}
public sealed class RequestFactory(string connection, DateTimeOffset instant, bool failLease = false) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:PropertyMaintenance"] = connection, ["Database:SeedDemoData"] = "false" }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new HttpClock(instant));
            if (failLease)
            {
                services.RemoveAll<ILeaseRepository>();
                services.AddScoped<ILeaseRepository, UnexpectedLeaseFailure>();
            }
        });
    }
}

public sealed class UnexpectedLeaseFailure : ILeaseRepository
{
    public Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Internal diagnostic fixture: must never reach HTTP clients.");
}

[Collection("SQL")]
public sealed class MaintenanceRequestHttpTests(SqlFixture fixture)
{
    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
    private static string Body(int unit, int tenant, string extra = "") =>
        $$"""{"unitId":{{unit}},"tenantId":{{tenant}},"category":" Plumbing ","priority":"High","description":" HTTP fixture "{{extra}}} """;

    [Fact]
    public async Task UnexpectedFailureReturnsSafe500Problem()
    {
        await using var db = fixture.Create();
        var lease = await db.Leases.FirstAsync();
        var before = await db.MaintenanceRequests.CountAsync();
        await using var factory = new RequestFactory(fixture.ConnectionString, DateTimeOffset.UtcNow, failLease: true);
        var response = await factory.CreateClient().PostAsync("/api/maintenance-requests", Json(Body(lease.UnitId, lease.TenantId)));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Internal diagnostic", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("UNEXPECTED_ERROR", JsonDocument.Parse(body).RootElement.GetProperty("code").GetString());
        Assert.Equal(before, await db.MaintenanceRequests.CountAsync());
    }

    [Theory]
    [InlineData("open-ended")]
    [InlineData("end-date")]
    public async Task ValidPostPersistsServerControlledValues(string scenario)
    {
        await using var db = fixture.Create();
        var lease = scenario == "open-ended" ? await db.Leases.FirstAsync(l => l.EndDate == null)
            : await db.Leases.FirstAsync(l => l.EndDate != null);
        var day = scenario == "end-date" ? lease.EndDate!.Value : lease.StartDate.AddDays(1);
        var instant = new DateTimeOffset(day.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero).AddTicks(9999999);
        await using var factory = new RequestFactory(fixture.ConnectionString, instant);
        using var client = factory.CreateClient();
        int? id = null;
        try
        {
            var response = await client.PostAsync("/api/maintenance-requests", Json(Body(lease.UnitId, lease.TenantId)));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.Null(response.Headers.Location);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            id = result.GetProperty("requestId").GetInt32();
            Assert.Equal("Open", result.GetProperty("status").GetString());
            Assert.Equal("High", result.GetProperty("priority").GetString());
            Assert.Equal(JsonValueKind.Null, result.GetProperty("technicianId").ValueKind);
            Assert.Equal(JsonValueKind.Null, result.GetProperty("closedAt").ValueKind);
            Assert.False(result.TryGetProperty("rowVersion", out _));
            var entity = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == id);
            Assert.Equal(lease.UnitId, entity.UnitId); Assert.Equal(lease.TenantId, entity.TenantId);
            Assert.Equal("Plumbing", entity.Category); Assert.Equal("HTTP fixture", entity.Description);
            Assert.Equal(1, entity.StatusId); Assert.Equal(3, entity.PriorityId);
            Assert.Equal(8, entity.RowVersion.Length);
            Assert.Equal(day, DateOnly.FromDateTime(entity.CreatedAt));
            Assert.Equal(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc), result.GetProperty("createdAt").GetDateTime());
        }
        finally
        {
            if (id.HasValue) await db.MaintenanceRequests.Where(r => r.RequestId == id).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData("unit", 404, "UNIT_NOT_FOUND")]
    [InlineData("tenant", 404, "TENANT_NOT_FOUND")]
    [InlineData("expired", 409, "ACTIVE_LEASE_REQUIRED")]
    [InlineData("future", 409, "ACTIVE_LEASE_REQUIRED")]
    [InlineData("other-unit", 409, "ACTIVE_LEASE_REQUIRED")]
    public async Task BusinessFailureReturnsProblemWithoutInsert(string scenario, int status, string code)
    {
        await using var db = fixture.Create();
        var lease = scenario == "future" ? await db.Leases.OrderByDescending(l => l.StartDate).FirstAsync()
            : await db.Leases.OrderBy(l => l.StartDate).FirstAsync();
        var day = scenario == "expired" ? lease.EndDate!.Value.AddDays(1) : lease.StartDate.AddDays(-1);
        var unit = scenario == "unit" ? int.MaxValue : lease.UnitId;
        var tenant = scenario == "tenant" ? int.MaxValue : lease.TenantId;
        if (scenario == "other-unit") unit = await db.Units.Where(u => !u.Leases.Any()).Select(u => u.UnitId).FirstAsync();
        var before = await db.MaintenanceRequests.CountAsync();
        await using var factory = new RequestFactory(fixture.ConnectionString, new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        var response = await factory.CreateClient().PostAsync("/api/maintenance-requests", Json(Body(unit, tenant)));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(before, await db.MaintenanceRequests.CountAsync());
    }

    [Theory]
    [InlineData("priority-invalid")]
    [InlineData("priority-missing")]
    [InlineData("priority-number")]
    [InlineData("priority-defined-number")]
    [InlineData("priority-number-string")]
    [InlineData("unit-invalid")]
    [InlineData("tenant-invalid")]
    [InlineData("category-empty")]
    [InlineData("description-empty")]
    [InlineData("category-long")]
    [InlineData("description-long")]
    [InlineData("malformed")]
    [InlineData("server-fields")]
    public async Task InvalidInputReturns400AndDoesNotInsert(string scenario)
    {
        await using var db = fixture.Create();
        var before = await db.MaintenanceRequests.CountAsync();
        var json = scenario switch
        {
            "priority-invalid" => Body(1,1).Replace("High", "Unknown"),
            "priority-missing" => Body(1,1).Replace("\"priority\":\"High\",", ""),
            "priority-number" => Body(1,1).Replace("\"High\"", "99"),
            "priority-defined-number" => Body(1,1).Replace("\"High\"", "1"),
            "priority-number-string" => Body(1,1).Replace("High", "99"),
            "unit-invalid" => Body(0,1), "tenant-invalid" => Body(1,-1),
            "category-empty" => Body(1,1).Replace(" Plumbing ", "   "),
            "description-empty" => Body(1,1).Replace(" HTTP fixture ", "   "),
            "category-long" => Body(1,1).Replace(" Plumbing ", new string('x',101)),
            "description-long" => Body(1,1).Replace(" HTTP fixture ", new string('x',2001)),
            "server-fields" => Body(1,1, ",\"requestId\":999,\"technicianId\":1,\"status\":\"Closed\",\"createdAt\":\"2000-01-01\",\"closedAt\":\"2000-01-02\",\"rowVersion\":\"AAAA\""),
            _ => "{invalid-json"
        };
        await using var factory = new RequestFactory(fixture.ConnectionString, DateTimeOffset.UtcNow);
        var response = await factory.CreateClient().PostAsync("/api/maintenance-requests", Json(json));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(400, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
        Assert.Equal(before, await db.MaintenanceRequests.CountAsync());
    }
}
