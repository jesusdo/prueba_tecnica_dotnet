using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.IntegrationTests;

internal static class VisitFixtures
{
    internal static readonly DateTimeOffset Now = new(2026, 11, 25, 18, 0, 0, TimeSpan.Zero);
    internal static async Task<(int Id, int Tech, int Other)> CreateAsync(SqlFixture fixture, byte status, bool withVisit = false)
    {
        await using var db = fixture.Create();
        var lease = await db.Leases.AsNoTracking().FirstAsync();
        var techs = await db.Technicians.Where(t => t.IsActive).OrderBy(t => t.TechnicianId).Select(t => t.TechnicianId).Take(2).ToArrayAsync();
        var request = new MaintenanceRequest { UnitId = lease.UnitId, TenantId = lease.TenantId,
            TechnicianId = status == 1 ? null : techs[0], Category = "T07 fixture", PriorityId = 1, StatusId = status,
            Description = "T07 isolated fixture", CreatedAt = Now.AddDays(-1).UtcDateTime,
            ClosedAt = status is 4 or 5 ? Now.AddHours(-1).UtcDateTime : null };
        db.MaintenanceRequests.Add(request); await db.SaveChangesAsync();
        if (withVisit)
        {
            db.Visits.Add(new Visit { RequestId = request.RequestId, TechnicianId = techs[0], Cost = 0,
                VisitedAt = Now.AddHours(-2).UtcDateTime, Notes = "T07 existing visit fixture" });
            await db.SaveChangesAsync();
        }
        return (request.RequestId, techs[0], techs[1]);
    }
    internal static async Task CleanAsync(SqlFixture fixture, int id)
    {
        await using var db = fixture.Create();
        await db.Visits.Where(v => v.RequestId == id).ExecuteDeleteAsync();
        await db.MaintenanceRequests.Where(r => r.RequestId == id).ExecuteDeleteAsync();
    }
    internal static StringContent Json(string text) => new(text, Encoding.UTF8, "application/json");
}

[Collection("SQL")]
public sealed class VisitHttpTests(SqlFixture fixture)
{
    [Theory]
    [InlineData(2, "0")]
    [InlineData(3, "123.45")]
    [InlineData(2, "9999999999999999.99")]
    [InlineData(3, "1e-2")]
    public async Task ValidVisitsPersistExactValuesAndLeaveParentUnchanged(byte status, string amount)
    {
        var row = await VisitFixtures.CreateAsync(fixture, status);
        try
        {
            await using var db = fixture.Create();
            var before = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            await using var factory = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now.AddTicks(12345));
            using var client = factory.CreateClient();
            for (var n = 0; n < 2; n++)
            {
                var response = await client.PostAsync($"/api/maintenance-requests/{row.Id}/visits",
                    VisitFixtures.Json($$"""{"technicianId":{{row.Other}},"notes":" Diagnostic ","cost":{{amount}}}"""));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.Null(response.Headers.Location);
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var saved = await db.Visits.AsNoTracking().SingleAsync(v => v.VisitId == result.GetProperty("visitId").GetInt32());
                Assert.Equal(row.Id, saved.RequestId); Assert.Equal(row.Other, saved.TechnicianId);
                Assert.Equal("Diagnostic", saved.Notes); Assert.Equal(result.GetProperty("cost").GetDecimal(), saved.Cost);
                var expected = decimal.Parse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(expected, saved.Cost); Assert.Equal(VisitFixtures.Now.UtcDateTime.AddMilliseconds(1), saved.VisitedAt);
                Assert.Equal(DateTimeKind.Utc, result.GetProperty("visitedAt").GetDateTime().Kind);
            }
            Assert.Equal(2, await db.Visits.CountAsync(v => v.RequestId == row.Id));
            var after = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(before.StatusId, after.StatusId); Assert.Equal(before.RowVersion, after.RowVersion);
            Assert.Equal(before.TechnicianId, after.TechnicianId); Assert.Equal(before.ClosedAt, after.ClosedAt);
        }
        finally { await VisitFixtures.CleanAsync(fixture, row.Id); }
    }

    [Theory]
    [InlineData("negative",400)] [InlineData("missing-cost",400)] [InlineData("null-cost",400)]
    [InlineData("scale",400)] [InlineData("precision",400)] [InlineData("extreme-scale",400)]
    [InlineData("notes-empty",400)] [InlineData("notes-long",400)] [InlineData("notes-missing",400)]
    [InlineData("missing-tech-id",400)] [InlineData("invalid-tech-id",400)] [InlineData("invalid-id",400)]
    [InlineData("server-fields",400)] [InlineData("request-body-id",400)] [InlineData("malformed",400)]
    [InlineData("missing-request",404)] [InlineData("missing-tech",404)] [InlineData("inactive",409)]
    [InlineData("open",409)] [InlineData("closed",409)] [InlineData("cancelled",409)]
    public async Task RejectedVisitReturnsProblemWithoutResiduals(string scenario, int expected)
    {
        var status = scenario switch { "open" => (byte)1, "closed" => (byte)4, "cancelled" => (byte)5, _ => (byte)2 };
        var row = await VisitFixtures.CreateAsync(fixture, status);
        try
        {
            await using var db = fixture.Create();
            var before = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            var count = await db.Visits.CountAsync();
            var technician = scenario == "missing-tech" ? int.MaxValue : scenario == "inactive"
                ? await db.Technicians.Where(t => !t.IsActive).Select(t => t.TechnicianId).FirstAsync() : row.Other;
            var body = $$"""{"technicianId":{{technician}},"notes":"Test","cost":1.23}""";
            body = scenario switch
            {
                "negative" => body.Replace("1.23", "-1"), "missing-cost" => body.Replace(",\"cost\":1.23", ""),
                "null-cost" => body.Replace("1.23", "null"), "scale" => body.Replace("1.23", "0.001"),
                "precision" => body.Replace("1.23", "10000000000000000"), "extreme-scale" => body.Replace("1.23", "1e-100"),
                "notes-empty" => body.Replace("Test", "   "), "notes-long" => body.Replace("Test", new string('x',2001)),
                "notes-missing" => body.Replace("\"notes\":\"Test\",", ""),
                "missing-tech-id" => "{\"notes\":\"Test\",\"cost\":0}",
                "invalid-tech-id" => "{\"technicianId\":0,\"notes\":\"Test\",\"cost\":0}",
                "server-fields" => body.Replace("}", ",\"visitId\":99,\"visitedAt\":\"2099-01-01\"}"),
                "request-body-id" => body.Replace("}", ",\"requestId\":99}"), "malformed" => "{broken", _ => body
            };
            await using var factory = new RequestFactory(fixture.ConnectionString, VisitFixtures.Now);
            var id = scenario == "missing-request" ? int.MaxValue : scenario == "invalid-id" ? 0 : row.Id;
            var response = await factory.CreateClient().PostAsync($"/api/maintenance-requests/{id}/visits", VisitFixtures.Json(body));
            Assert.Equal(expected, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(expected, problem.GetProperty("status").GetInt32());
            var code = scenario switch { "missing-request" => "REQUEST_NOT_FOUND", "missing-tech" => "TECHNICIAN_NOT_FOUND",
                "inactive" => "TECHNICIAN_INACTIVE", "open" or "closed" or "cancelled" => "INVALID_VISIT_STATUS", _ => null };
            if (code is not null) Assert.Equal(code, problem.GetProperty("code").GetString());
            Assert.Equal(count, await db.Visits.CountAsync());
            var after = await db.MaintenanceRequests.AsNoTracking().SingleAsync(r => r.RequestId == row.Id);
            Assert.Equal(before.RowVersion, after.RowVersion); Assert.Equal(before.StatusId, after.StatusId);
        }
        finally { await VisitFixtures.CleanAsync(fixture, row.Id); }
    }
}
