using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyMaintenance.Api.DataAccess.Entities;

namespace PropertyMaintenance.IntegrationTests;

[Collection("SQL")]
public sealed class LeaseBatchRegressionTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("shift", true)]
    [InlineData("swap-periods", true)]
    [InlineData("swap-units", true)]
    [InlineData("invalid-batch", false)]
    [InlineData("unaffected-conflict", false)]
    [InlineData("move-conflict", false)]
    [InlineData("inclusive-boundary", false)]
    [InlineData("indefinite-conflict", false)]
    [InlineData("single-update", true)]
    [InlineData("batch-rent", true)]
    [InlineData("single-insert", true)]
    [InlineData("insert-boundary", false)]
    [InlineData("insert-indefinite", true)]
    [InlineData("batch-insert", true)]
    [InlineData("invalid-insert-batch", false)]
    [InlineData("update-indefinite", true)]
    public async Task LeaseWritesValidateFinalBatchAndPreserveExistingRules(string scenario, bool accepted)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        async Task<string> Execute(string sql)
        {
            await using var command = new SqlCommand(sql, connection, transaction);
            return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
        }

        var marker = "T102-" + Guid.NewGuid().ToString("N");
        await Execute("CREATE TABLE #Fixture(UnitA int, UnitB int, TenantId int);");
        await using (var setup = new SqlCommand("""
            INSERT dbo.Properties(Name,Address,City) VALUES(@marker,N'Test',N'Test');
            DECLARE @Property int=SCOPE_IDENTITY(), @UnitA int, @UnitB int, @Tenant int;
            INSERT dbo.Units(PropertyId,Number,Floor,UnitTypeId) VALUES(@Property,N'A',0,1);
            SET @UnitA=SCOPE_IDENTITY();
            INSERT dbo.Units(PropertyId,Number,Floor,UnitTypeId) VALUES(@Property,N'B',0,1);
            SET @UnitB=SCOPE_IDENTITY();
            INSERT dbo.Tenants(FullName,Email,Phone) VALUES(@marker,@marker+N'@example.test',N'000');
            SET @Tenant=SCOPE_IDENTITY();
            INSERT #Fixture VALUES(@UnitA,@UnitB,@Tenant);
            """, connection, transaction))
        {
            setup.Parameters.AddWithValue("@marker", marker);
            await setup.ExecuteNonQueryAsync();
        }

        var separateUnits = scenario is "swap-units" or "move-conflict";
        await Execute("""
            DECLARE @A int, @B int, @T int;
            SELECT @A=UnitA,@B=UnitB,@T=TenantId FROM #Fixture;
            INSERT dbo.Leases(UnitId,TenantId,StartDate,EndDate,MonthlyRent)
            VALUES(@A,@T,'20250101','20250131',1000),
            """ + (separateUnits
                ? "(@B,@T,'20250101','20250131',1000);"
                : "(@A,@T,'20250201','20250228',1000);"));

        const string declarations = "DECLARE @A int,@B int,@T int; SELECT @A=UnitA,@B=UnitB,@T=TenantId FROM #Fixture; ";
        var originalFirstId = int.Parse(await Execute("SELECT LeaseId FROM dbo.Leases WHERE UnitId=(SELECT UnitA FROM #Fixture) AND StartDate='20250101';"));
        var originalOtherId = int.Parse(await Execute("SELECT LeaseId FROM dbo.Leases l CROSS JOIN #Fixture f WHERE l.UnitId IN (f.UnitA,f.UnitB) AND NOT(l.UnitId=f.UnitA AND StartDate='20250101');"));
        const string insert = "INSERT dbo.Leases(UnitId,TenantId,StartDate,EndDate,MonthlyRent) VALUES ";
        var mutation = scenario switch
        {
            "shift" => "UPDATE dbo.Leases SET StartDate=DATEADD(MONTH,1,StartDate), EndDate=EOMONTH(DATEADD(MONTH,1,StartDate)) WHERE UnitId=@A;",
            "swap-periods" => "UPDATE dbo.Leases SET StartDate=CASE WHEN MONTH(StartDate)=1 THEN '20250201' ELSE '20250101' END, EndDate=CASE WHEN MONTH(StartDate)=1 THEN '20250228' ELSE '20250131' END WHERE UnitId=@A;",
            "swap-units" => "UPDATE dbo.Leases SET UnitId=CASE WHEN UnitId=@A THEN @B ELSE @A END WHERE UnitId IN (@A,@B);",
            "invalid-batch" => "UPDATE dbo.Leases SET StartDate='20250301',EndDate='20250331' WHERE UnitId=@A;",
            "unaffected-conflict" => "UPDATE dbo.Leases SET StartDate='20250201',EndDate='20250228' WHERE UnitId=@A AND StartDate='20250101';",
            "move-conflict" => "UPDATE dbo.Leases SET UnitId=@B WHERE UnitId=@A;",
            "inclusive-boundary" => "UPDATE dbo.Leases SET EndDate='20250201' WHERE UnitId=@A AND StartDate='20250101';",
            "indefinite-conflict" => "UPDATE dbo.Leases SET EndDate=NULL WHERE UnitId=@A AND StartDate='20250101';",
            "single-update" => "UPDATE dbo.Leases SET EndDate='20250130' WHERE UnitId=@A AND StartDate='20250101';",
            "batch-rent" => "UPDATE dbo.Leases SET MonthlyRent=1100 WHERE UnitId=@A;",
            "single-insert" => insert + "(@A,@T,'20250301','20250331',1000);",
            "insert-boundary" => insert + "(@A,@T,'20250131','20250131',1000);",
            "insert-indefinite" => insert + "(@A,@T,'20250301',NULL,1000);",
            "batch-insert" => insert + "(@A,@T,'20250301','20250331',1000),(@A,@T,'20250401','20250430',1000);",
            "invalid-insert-batch" => insert + "(@A,@T,'20250301','20250331',1000),(@A,@T,'20250331','20250430',1000);",
            "update-indefinite" => "UPDATE dbo.Leases SET EndDate=NULL WHERE UnitId=@A AND StartDate='20250201';",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        if (!accepted)
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => Execute(declarations + mutation));
            Assert.Equal(51021, error.Number);
            Assert.Equal("Lease period overlaps another lease for the same unit.", error.Message);
            // THROW inside the trigger rolls back the statement or enclosing transaction.
            if (transaction.Connection is not null) await transaction.RollbackAsync();
        }
        else
        {
            await Execute(declarations + mutation);
            var state = await Execute("""
                SELECT LeaseId,CASE WHEN l.UnitId=f.UnitA THEN 'A' ELSE 'B' END AS Unit,
                    CONVERT(char(10),StartDate,23) AS StartDate,
                    CONVERT(char(10),EndDate,23) AS EndDate,MonthlyRent
                FROM dbo.Leases l CROSS JOIN #Fixture f WHERE l.UnitId IN (f.UnitA,f.UnitB)
                ORDER BY Unit,StartDate FOR JSON PATH,INCLUDE_NULL_VALUES;
                """);
            var rows = System.Text.Json.JsonDocument.Parse(state).RootElement;
            Assert.Equal(scenario == "batch-insert" ? 4 : scenario.Contains("insert") ? 3 : 2, rows.GetArrayLength());
            var first = rows[0];
            var last = rows[rows.GetArrayLength() - 1];
            Assert.Equal(scenario is "swap-periods" or "swap-units" ? originalOtherId : originalFirstId,
                first.GetProperty("LeaseId").GetInt32());
            Assert.Equal(scenario == "shift" ? "2025-02-01" : "2025-01-01", first.GetProperty("StartDate").GetString());
            Assert.Equal(scenario == "batch-rent" ? 1100m : 1000m, first.GetProperty("MonthlyRent").GetDecimal());
            Assert.Equal(scenario == "single-update" ? "2025-01-30" : scenario == "shift" ? "2025-02-28" : "2025-01-31", first.GetProperty("EndDate").GetString());
            Assert.Equal(scenario switch
            {
                "shift" or "single-insert" or "insert-indefinite" => "2025-03-01",
                "batch-insert" => "2025-04-01",
                "swap-units" => "2025-01-01",
                _ => "2025-02-01"
            }, last.GetProperty("StartDate").GetString());
            if (scenario is "insert-indefinite" or "update-indefinite")
                Assert.Equal(System.Text.Json.JsonValueKind.Null, last.GetProperty("EndDate").ValueKind);
            else Assert.Equal(scenario switch
            {
                "shift" or "single-insert" => "2025-03-31",
                "batch-insert" => "2025-04-30",
                "swap-units" => "2025-01-31",
                _ => "2025-02-28"
            }, last.GetProperty("EndDate").GetString());
            await transaction.RollbackAsync();
        }

        await using var verify = new SqlCommand("SELECT COUNT(*) FROM dbo.Properties WHERE Name=@marker", connection);
        verify.Parameters.AddWithValue("@marker", marker);
        Assert.Equal(0, (int)(await verify.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ExistingEfLeaseUpdateRemainsCompatibleWithTrigger()
    {
        await using var db = fixture.Create();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var lease = await db.Leases.FirstAsync();
        var expected = lease.MonthlyRent + 1;
        lease.MonthlyRent = expected;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(expected, await db.Leases.Where(l => l.LeaseId == lease.LeaseId).Select(l => l.MonthlyRent).SingleAsync());
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentInsertWaitsForValidBatchShiftThenValidatesCommittedPeriods(bool compatible)
    {
        await using var sessionA = new SqlConnection(fixture.ConnectionString);
        await using var sessionB = new SqlConnection(fixture.ConnectionString);
        await using var observer = new SqlConnection(fixture.ConnectionString);
        await sessionA.OpenAsync();
        await sessionB.OpenAsync();
        await observer.OpenAsync();
        var marker = "T102-Concurrent-" + Guid.NewGuid().ToString("N");
        SqlTransaction? transactionA = null;
        Task<int>? pendingInsert = null;
        await using var insert = sessionB.CreateCommand();
        insert.CommandTimeout = 15;
        try
        {
            await using var setup = observer.CreateCommand();
            setup.CommandText = """
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                INSERT dbo.Properties(Name,Address,City) VALUES(@marker,N'Test',N'Test');
                DECLARE @P int=SCOPE_IDENTITY(),@U int,@T int;
                INSERT dbo.Units(PropertyId,Number,Floor,UnitTypeId) VALUES(@P,N'A',0,1);
                SET @U=SCOPE_IDENTITY();
                INSERT dbo.Tenants(FullName,Email,Phone) VALUES(@marker,@marker+N'@example.test',N'000');
                SET @T=SCOPE_IDENTITY();
                INSERT dbo.Leases(UnitId,TenantId,StartDate,EndDate,MonthlyRent)
                VALUES(@U,@T,'20250101','20250131',1000),(@U,@T,'20250201','20250228',1000);
                COMMIT;
                SELECT @U,@T;
                """;
            setup.Parameters.AddWithValue("@marker", marker);
            int unitId, tenantId;
            await using (var reader = await setup.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                unitId = reader.GetInt32(0);
                tenantId = reader.GetInt32(1);
            }
            await using var identityA = new SqlCommand("SELECT @@SPID", sessionA);
            await using var identityB = new SqlCommand("SELECT @@SPID", sessionB);
            var spidA = (short)(await identityA.ExecuteScalarAsync())!;
            var spidB = (short)(await identityB.ExecuteScalarAsync())!;
            Assert.NotEqual(spidA, spidB);

            transactionA = (SqlTransaction)await sessionA.BeginTransactionAsync();
            await using (var shift = new SqlCommand("""
                UPDATE dbo.Leases SET StartDate=DATEADD(MONTH,1,StartDate),
                    EndDate=EOMONTH(DATEADD(MONTH,1,StartDate)) WHERE UnitId=@unit;
                """, sessionA, transactionA))
            {
                shift.Parameters.AddWithValue("@unit", unitId);
                await shift.ExecuteNonQueryAsync();
            }
            insert.CommandText = """
                SET XACT_ABORT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;
                    INSERT dbo.Leases(UnitId,TenantId,StartDate,EndDate,MonthlyRent)
                    VALUES(@unit,@tenant,@start,@end,1000);
                    COMMIT;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT>0 ROLLBACK;
                    THROW;
                END CATCH;
                """;
            insert.Parameters.AddWithValue("@unit", unitId);
            insert.Parameters.AddWithValue("@tenant", tenantId);
            insert.Parameters.Add("@start", System.Data.SqlDbType.Date).Value = new DateTime(2025, compatible ? 4 : 2, 1);
            insert.Parameters.Add("@end", System.Data.SqlDbType.Date).Value = new DateTime(2025, compatible ? 4 : 2, compatible ? 30 : 28);
            pendingInsert = insert.ExecuteNonQueryAsync();
            await using var blocking = observer.CreateCommand();
            blocking.CommandTimeout = 2;
            blocking.CommandText = """
                SELECT COUNT(*) FROM sys.dm_exec_requests
                WHERE session_id=@b AND blocking_session_id=@a AND wait_type LIKE N'LCK_M_%';
                """;
            blocking.Parameters.AddWithValue("@a", spidA);
            blocking.Parameters.AddWithValue("@b", spidB);
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var observedBlocking = false;
            while (elapsed.Elapsed < TimeSpan.FromSeconds(3) && !pendingInsert.IsCompleted)
            {
                if ((int)(await blocking.ExecuteScalarAsync())! == 1) { observedBlocking = true; break; }
                await Task.Delay(25);
            }
            Assert.True(observedBlocking, "Session B must be observed waiting on session A before its commit.");
            Assert.False(pendingInsert.IsCompleted);
            await transactionA.CommitAsync();
            if (compatible) await pendingInsert;
            else
            {
                var error = await Assert.ThrowsAsync<SqlException>(async () => await pendingInsert);
                Assert.Equal(51021, error.Number);
            }
            await using var verify = observer.CreateCommand();
            verify.Parameters.AddWithValue("@unit", unitId);
            verify.CommandText = "SELECT COUNT(*) FROM dbo.Leases WHERE UnitId=@unit";
            Assert.Equal(compatible ? 3 : 2, (int)(await verify.ExecuteScalarAsync())!);
            verify.CommandText = """
                SELECT COUNT(*) FROM dbo.Leases WHERE UnitId=@unit AND
                  ((StartDate='20250201' AND EndDate='20250228') OR
                   (StartDate='20250301' AND EndDate='20250331'));
                """;
            Assert.Equal(2, (int)(await verify.ExecuteScalarAsync())!);
            verify.CommandText = """
                SELECT COUNT(*) FROM dbo.Leases a JOIN dbo.Leases b ON a.UnitId=b.UnitId AND a.LeaseId<b.LeaseId
                AND a.StartDate<=ISNULL(b.EndDate,CONVERT(date,'99991231'))
                AND b.StartDate<=ISNULL(a.EndDate,CONVERT(date,'99991231')) WHERE a.UnitId=@unit;
                """;
            Assert.Equal(0, (int)(await verify.ExecuteScalarAsync())!);
        }
        finally
        {
            if (transactionA?.Connection is not null) await transactionA.RollbackAsync();
            if (pendingInsert is not null)
            {
                if (!pendingInsert.IsCompleted) insert.Cancel();
                try { await pendingInsert; } catch (SqlException) { }
            }
            if (transactionA is not null) await transactionA.DisposeAsync();
            await using var cleanup = observer.CreateCommand();
            cleanup.CommandText = """
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                DELETE l FROM dbo.Leases l JOIN dbo.Units u ON u.UnitId=l.UnitId
                    JOIN dbo.Properties p ON p.PropertyId=u.PropertyId WHERE p.Name=@marker;
                DELETE u FROM dbo.Units u JOIN dbo.Properties p ON p.PropertyId=u.PropertyId WHERE p.Name=@marker;
                DELETE dbo.Properties WHERE Name=@marker;
                DELETE dbo.Tenants WHERE Email=@marker+N'@example.test';
                COMMIT;
                SELECT COUNT(*) FROM dbo.Properties WHERE Name=@marker;
                """;
            cleanup.Parameters.AddWithValue("@marker", marker);
            Assert.Equal(0, (int)(await cleanup.ExecuteScalarAsync())!);
        }
    }
}
