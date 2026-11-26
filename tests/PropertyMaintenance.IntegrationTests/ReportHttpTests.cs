using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyMaintenance.Api.DataAccess.Context;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class ReportHttpTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("/api/reports/properties-summary")]
    [InlineData("/api/reports/properties-summary?from=2026-01-01")]
    [InlineData("/api/reports/properties-summary?to=2026-01-01")]
    [InlineData("/api/reports/properties-summary?from=bad&to=2026-01-01")]
    [InlineData("/api/reports/properties-summary?from=2026-02-02&to=2026-01-01")]
    [InlineData("/api/reports/properties-summary?from=9999-12-31&to=9999-12-31")]
    public async Task InvalidDatesReturn400Problem(string url)
    {
        await using var factory = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
        var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/api/reports/properties-summary?from=0001-01-01&to=2026-12-31", "propertyId")]
    [InlineData("/api/reports/technicians-workload", "technicianId")]
    public async Task ReportsReturnArrays(string url, string id)
    {
        await using var factory = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
        var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.NotEmpty(result.EnumerateArray());
        Assert.All(result.EnumerateArray(), row => Assert.True(row.GetProperty(id).GetInt32() > 0));
    }

    [Fact]
    public async Task PropertyIndicatorsMatchControlledFixturesAndPreserveNullAverage()
    {
        await using var data = await ReadFixtures.CreateAsync(fixture);
        await using var factory = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
        var rows = await factory.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/reports/properties-summary?from=2026-01-01&to=2026-12-31");
        var property = rows.EnumerateArray().Single(r => r.GetProperty("propertyId").GetInt32() == data.PropertyId);
        Assert.Equal(3, property.GetProperty("openRequests").GetInt32());
        Assert.Equal(1, property.GetProperty("closedRequests").GetInt32());
        Assert.Equal(24m, property.GetProperty("avgResolutionHours").GetDecimal());
        Assert.Equal(30.35m, property.GetProperty("totalVisitCost").GetDecimal());
        var empty = rows.EnumerateArray().Single(r => r.GetProperty("propertyId").GetInt32() == data.EmptyPropertyId);
        Assert.Equal(0, empty.GetProperty("openRequests").GetInt32());
        Assert.Equal(0, empty.GetProperty("closedRequests").GetInt32());
        Assert.Equal(0m, empty.GetProperty("totalVisitCost").GetDecimal());
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("avgResolutionHours").ValueKind);
    }

    [Fact]
    public async Task HttpExecutesParameterizedProceduresWithoutComposition()
    {
        var capture = new SqlCapture();
        await using var root = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
        await using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<PropertyMaintenanceDbContext>(options => options.AddInterceptors(capture))));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/reports/properties-summary?from=2026-01-01&to=2026-12-31")).StatusCode);
        Assert.Single(capture.Commands);
        Assert.StartsWith("EXEC dbo.usp_GetPropertiesSummary", capture.Commands[0]);
        Assert.Contains("@From=", capture.Commands[0]);
        capture.Commands.Clear();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/reports/technicians-workload")).StatusCode);
        Assert.Single(capture.Commands);
        Assert.StartsWith("EXEC dbo.usp_GetTechniciansWorkload", capture.Commands[0]);
        Assert.Contains("@AsOfUtc=", capture.Commands[0]);
    }

    [Fact]
    public async Task CurrentSqlOrderingAndNullCostArePreservedWithoutCorrectingIndicators()
    {
        await using var db = fixture.Create();
        var tech = new PropertyMaintenance.Api.DataAccess.Entities.Technician
        { Name = "T09 no visits", SpecialtyId = 3, IsActive = true };
        db.Technicians.Add(tech); await db.SaveChangesAsync();
        var unit = await db.Units.FirstAsync();
        var tenant = await db.Tenants.FirstAsync();
        var request = new PropertyMaintenance.Api.DataAccess.Entities.MaintenanceRequest
        { UnitId = unit.UnitId, TenantId = tenant.TenantId, TechnicianId = tech.TechnicianId,
            StatusId = 2, PriorityId = 1, Category = "T09", Description = "T09 report fixture" };
        try
        {
            db.MaintenanceRequests.Add(request); await db.SaveChangesAsync();
            await using var factory = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
            var rows = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/reports/technicians-workload");
            var actual = rows.EnumerateArray().ToArray();
            var added = actual.Single(r => r.GetProperty("technicianId").GetInt32() == tech.TechnicianId);
            Assert.Equal(1, added.GetProperty("assignedRequests").GetInt32());
            Assert.Equal(0, added.GetProperty("inProgressRequests").GetInt32());
            Assert.Equal(1, added.GetProperty("totalWorkload").GetInt32());
            Assert.Equal(0, added.GetProperty("visitsLastMonth").GetInt32());
            Assert.Equal(JsonValueKind.Null, added.GetProperty("totalVisitCost").ValueKind);
            // Independent expression of the current SQL ordering, including its known visit multiplication.
            var expected = await db.Database.SqlQuery<int>($"""
                SELECT t.TechnicianId AS Value FROM dbo.Technicians t
                JOIN dbo.MaintenanceRequests r ON r.TechnicianId=t.TechnicianId AND r.StatusId IN (2,3)
                LEFT JOIN dbo.Visits v ON v.RequestId=r.RequestId AND v.TechnicianId=t.TechnicianId
                WHERE t.IsActive=1 GROUP BY t.TechnicianId
                ORDER BY COUNT(r.RequestId) DESC,t.TechnicianId
                """).ToArrayAsync();
            Assert.Equal(expected, actual.Select(r => r.GetProperty("technicianId").GetInt32()));
        }
        finally
        {
            await db.MaintenanceRequests.Where(r => r.RequestId == request.RequestId).ExecuteDeleteAsync();
            await db.Technicians.Where(t => t.TechnicianId == tech.TechnicianId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task OpenApiDocumentsAllEightOperations()
    {
        await using var factory = new RequestFactory(fixture.ConnectionString, ReadFixtures.Now);
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var operations = document.GetProperty("paths").EnumerateObject()
            .SelectMany(p => p.Value.EnumerateObject().Where(m => m.Name is "get" or "post" or "patch").Select(m => (p.Name, m.Name))).ToArray();
        Assert.Equal(8, operations.Length);
        Assert.Contains(("/api/reports/properties-summary", "get"), operations);
        Assert.Contains(("/api/reports/technicians-workload", "get"), operations);
    }
}
