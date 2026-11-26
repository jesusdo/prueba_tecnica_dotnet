using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.IntegrationTests;

internal sealed class SqlCapture : DbCommandInterceptor
{
    public List<string> Commands { get; } = [];
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Commands.Add(command.CommandText); return ValueTask.FromResult(result); }
}

internal sealed class ReadFixtures(SqlFixture fixture) : IAsyncDisposable
{
    internal static readonly DateTimeOffset Now = new(2026,11,20,12,0,0,TimeSpan.Zero);
    internal int PropertyId, EmptyPropertyId, TenantId, OtherTenantId, EmptyTenantId;
    internal int HistoricalRequest, FirstTiedRequest, SecondTiedRequest, NextDayRequest;
    internal int[] UnitIds = [];

    internal static async Task<ReadFixtures> CreateAsync(SqlFixture fixture)
    {
        var result = new ReadFixtures(fixture);
        await using var db = fixture.Create();
        var property = new Property { Name="T08 Property",Address="Demo Street",City="Demo City" };
        var empty = new Property { Name="T08 Empty",Address="Demo Street",City="Demo City" };
        var key = Guid.NewGuid().ToString("N");
        var tenant = new Tenant { FullName="T08 Tenant",Email=$"t08.{key}@example.test",Phone="555-0100" };
        var other = new Tenant { FullName="T08 Other",Email=$"t08.other.{key}@example.test",Phone="555-0101" };
        var emptyTenant = new Tenant { FullName="T08 Empty Tenant",Email=$"t08.empty.{key}@example.test",Phone="555-0102" };
        db.AddRange(property,empty,tenant,other,emptyTenant); await db.SaveChangesAsync();
        result.PropertyId=property.PropertyId; result.EmptyPropertyId=empty.PropertyId;
        result.TenantId=tenant.TenantId; result.OtherTenantId=other.TenantId; result.EmptyTenantId=emptyTenant.TenantId;
        var units = Enumerable.Range(1,5).Select(n=>new Unit { PropertyId=property.PropertyId, Number=n==1?"U10":$"U{n}",Floor=(short)n,UnitTypeId=(byte)(n%2+1) }).ToArray();
        db.Units.AddRange(units); await db.SaveChangesAsync(); result.UnitIds=units.Select(u=>u.UnitId).ToArray();
        var today=DateOnly.FromDateTime(Now.UtcDateTime);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.Leases(UnitId,TenantId,StartDate,EndDate,MonthlyRent) VALUES({units[0].UnitId},{tenant.TenantId},{today.AddDays(-10)},{today},{100m}),({units[1].UnitId},{tenant.TenantId},{today.AddDays(-100)},{today.AddDays(-50)},{200m}),({units[2].UnitId},{tenant.TenantId},{today.AddDays(1)},{today.AddDays(30)},{300m}),({units[3].UnitId},{other.TenantId},{today.AddDays(-10)},{(DateOnly?)null},{400m})");
        var tech=await db.Technicians.Where(t=>t.IsActive).Select(t=>t.TechnicianId).FirstAsync();
        MaintenanceRequest Make(int unit, DateTime time, byte priority=1, byte status=1) => new()
        { UnitId=unit,TenantId=tenant.TenantId,TechnicianId=status==4?tech:null,Category="T08 Category",PriorityId=priority,StatusId=status,
            Description="T08 request fixture",CreatedAt=time,ClosedAt=status==4?time.AddDays(1):null };
        var historical=Make(units[1].UnitId,Now.AddDays(-80).UtcDateTime,3,4);
        var first=Make(units[0].UnitId,Now.UtcDateTime,3);
        var second=Make(units[0].UnitId,Now.UtcDateTime,1);
        var next=Make(units[2].UnitId,today.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc),3);
        db.AddRange(historical,first,second,next); await db.SaveChangesAsync();
        result.HistoricalRequest=historical.RequestId; result.FirstTiedRequest=first.RequestId;
        result.SecondTiedRequest=second.RequestId; result.NextDayRequest=next.RequestId;
        db.Visits.AddRange(new Visit { RequestId=historical.RequestId,TechnicianId=tech,VisitedAt=historical.CreatedAt.AddHours(1),Notes="T08 visit",Cost=10.10m },
            new Visit { RequestId=historical.RequestId,TechnicianId=tech,VisitedAt=historical.CreatedAt.AddHours(2),Notes="T08 visit",Cost=20.25m });
        await db.SaveChangesAsync();
        return result;
    }
    public async ValueTask DisposeAsync()
    {
        await using var db=fixture.Create();
        await db.Visits.Where(v=>v.Request.Unit.PropertyId==PropertyId).ExecuteDeleteAsync();
        await db.MaintenanceRequests.Where(r=>r.Unit.PropertyId==PropertyId).ExecuteDeleteAsync();
        await db.Leases.Where(l=>l.Unit.PropertyId==PropertyId).ExecuteDeleteAsync();
        await db.Units.Where(u=>u.PropertyId==PropertyId).ExecuteDeleteAsync();
        await db.Properties.Where(p=>p.PropertyId==PropertyId||p.PropertyId==EmptyPropertyId).ExecuteDeleteAsync();
        await db.Tenants.Where(t=>t.TenantId==TenantId||t.TenantId==OtherTenantId||t.TenantId==EmptyTenantId).ExecuteDeleteAsync();
    }
}
