-- Run only against an isolated database initialized by DbUp. Never edits procedures.
-- Specification failures are intentional evidence of accepted manual-SQL debt.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE N'PropertyMaintenanceDb_T04[_]%'
    THROW 51909, 'Report validation requires an isolated T04 test database.', 1;

DECLARE @Results TABLE (TestName nvarchar(200), Passed bit, KnownDebt bit);
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @P int, @Empty int, @Unit int, @Tenant int, @Tech int, @Idle int, @Inactive int;
    INSERT dbo.Properties(Name,Address,City) VALUES(N'T09 SQL fixture',N'Demo',N'Demo'); SET @P=SCOPE_IDENTITY();
    INSERT dbo.Properties(Name,Address,City) VALUES(N'T09 SQL empty',N'Demo',N'Demo'); SET @Empty=SCOPE_IDENTITY();
    INSERT dbo.Units(PropertyId,Number,Floor,UnitTypeId) VALUES(@P,N'T09',0,1); SET @Unit=SCOPE_IDENTITY();
    INSERT dbo.Tenants(FullName,Email,Phone) VALUES(N'T09 SQL tenant',N't09-sql@example.com',N'000'); SET @Tenant=SCOPE_IDENTITY();
    INSERT dbo.Technicians(Name,SpecialtyId,IsActive) VALUES(N'T09 SQL active',3,1); SET @Tech=SCOPE_IDENTITY();
    INSERT dbo.Technicians(Name,SpecialtyId,IsActive) VALUES(N'T09 SQL idle',3,1); SET @Idle=SCOPE_IDENTITY();
    INSERT dbo.Technicians(Name,SpecialtyId,IsActive) VALUES(N'T09 SQL inactive',3,0); SET @Inactive=SCOPE_IDENTITY();
    DECLARE @Requests TABLE (Label nvarchar(30), RequestId int);
    INSERT dbo.MaintenanceRequests(UnitId,TenantId,TechnicianId,Category,PriorityId,StatusId,Description,CreatedAt,ClosedAt)
    OUTPUT inserted.Description,inserted.RequestId INTO @Requests
    VALUES
      (@Unit,@Tenant,NULL,N'T09',1,1,N'open-start','2026-01-10',NULL),
      (@Unit,@Tenant,NULL,N'T09',1,1,N'open-last','2026-01-10T23:59:59.999',NULL),
      (@Unit,@Tenant,NULL,N'T09',1,1,N'open-next','2026-01-11',NULL),
      (@Unit,@Tenant,@Tech,N'T09',1,4,N'closed-in','2026-01-10T12:00:00','2026-01-10T13:30:00'),
      (@Unit,@Tenant,@Tech,N'T09',1,4,N'closed-before','2026-01-09T23:00:00','2026-01-10T00:30:00'),
      (@Unit,@Tenant,@Tech,N'T09',1,4,N'closed-out','2026-01-10','2026-01-11'),
      (@Unit,@Tenant,NULL,N'T09',1,5,N'cancelled','2026-01-10','2026-01-10T15:00:00'),
      (@Unit,@Tenant,@Tech,N'T09',1,2,N'old-pending','2025-12-01',NULL),
      (@Unit,@Tenant,@Tech,N'T09',1,3,N'in-progress','2026-01-10',NULL),
      (@Unit,@Tenant,@Inactive,N'T09',1,2,N'inactive-pending','2026-01-10',NULL);
    INSERT dbo.Visits(RequestId,TechnicianId,VisitedAt,Notes,Cost)
    SELECT RequestId,@Tech,'2026-01-10T12:30:00',N'T09 SQL',10.10 FROM @Requests WHERE Label=N'closed-in'
    UNION ALL SELECT RequestId,@Tech,'2026-01-10T13:00:00',N'T09 SQL',20.25 FROM @Requests WHERE Label=N'closed-in'
    UNION ALL SELECT RequestId,@Tech,'2026-01-12',N'T09 SQL',100 FROM @Requests WHERE Label=N'closed-out'
    UNION ALL SELECT RequestId,@Tech,'2026-01-10T10:00:00',N'T09 SQL',50 FROM @Requests WHERE Label=N'old-pending'
    UNION ALL SELECT RequestId,@Idle,'2026-01-10T11:00:00',N'T09 SQL',7 FROM @Requests WHERE Label=N'in-progress';

    DECLARE @PResult TABLE(PropertyId int,PropertyName nvarchar(200),OpenRequests int,ClosedRequests int,AvgResolutionHours int NULL,TotalVisitCost decimal(38,2));
    INSERT @PResult EXEC dbo.usp_GetPropertiesSummary @From='2026-01-10',@To='2026-01-10';
    INSERT @Results SELECT N'P01 one row per property',IIF(COUNT(*)=1,1,0),0 FROM @PResult WHERE PropertyId=@P;
    INSERT @Results SELECT N'P02 open day boundaries and current states',IIF(OpenRequests=4,1,0),0 FROM @PResult WHERE PropertyId=@P;
    INSERT @Results SELECT N'P03 closed by ClosedAt only; cancellation excluded',IIF(ClosedRequests=2,1,0),1 FROM @PResult WHERE PropertyId=@P;
    INSERT @Results SELECT N'P04 fractional average 1.5 hours',IIF(AvgResolutionHours=1.5,1,0),1 FROM @PResult WHERE PropertyId=@P;
    INSERT @Results SELECT N'P05 all visits in range including old pending request',IIF(TotalVisitCost=87.35,1,0),1 FROM @PResult WHERE PropertyId=@P;
    INSERT @Results SELECT N'P06 empty property zero counts/cost and NULL average',IIF(OpenRequests=0 AND ClosedRequests=0 AND TotalVisitCost=0 AND AvgResolutionHours IS NULL,1,0),0 FROM @PResult WHERE PropertyId=@Empty;
    BEGIN TRY
        INSERT @PResult EXEC dbo.usp_GetPropertiesSummary @From='2026-01-11',@To='2026-01-10';
        INSERT @Results VALUES(N'P07 SQL rejects inverted range',0,1);
    END TRY
    BEGIN CATCH
        -- No catch-all exception is counted as proof of a defined SQL validation contract.
        THROW;
    END CATCH;

    DECLARE @TResult TABLE(TechnicianId int,TechnicianName nvarchar(200),AssignedRequests int,InProgressRequests int,TotalWorkload int,VisitsLastMonth int,TotalVisitCost decimal(38,2) NULL);
    INSERT @TResult EXEC dbo.usp_GetTechniciansWorkload @AsOfUtc='2026-01-11';
    INSERT @Results SELECT N'T01 active technician represented',IIF(COUNT(*)=1,1,0),0 FROM @TResult WHERE TechnicianId=@Tech;
    INSERT @Results SELECT N'T02 inactive technician excluded',IIF(COUNT(*)=0,1,0),0 FROM @TResult WHERE TechnicianId=@Inactive;
    INSERT @Results SELECT N'T03 assigned and progress workload without closed rows',IIF(AssignedRequests=1 AND InProgressRequests=1 AND TotalWorkload=2,1,0),0 FROM @TResult WHERE TechnicianId=@Tech;
    INSERT @Results SELECT N'T04 active idle technician preserved',IIF(COUNT(*)=1,1,0),1 FROM @TResult WHERE TechnicianId=@Idle;
    INSERT @Results SELECT N'T05 technician historical cost includes closed visits',IIF(TotalVisitCost=80.35,1,0),1 FROM @TResult WHERE TechnicianId=@Tech;
    INSERT @Results SELECT N'T06 actual visiting technician gets cost',IIF(COUNT(*)=1 AND MAX(TotalVisitCost)=7,1,0),1 FROM @TResult WHERE TechnicianId=@Idle;
    INSERT @Results SELECT N'T07 fixed AsOfUtc controls recent visits',IIF(VisitsLastMonth=3,1,0),1 FROM @TResult WHERE TechnicianId=@Tech;

    -- Add two visits to one assigned request: the required load must stay one.
    INSERT dbo.Visits(RequestId,TechnicianId,VisitedAt,Notes,Cost)
    SELECT RequestId,@Tech,'2026-01-10T12:00:00',N'T09 SQL',1 FROM @Requests WHERE Label=N'old-pending'
    UNION ALL SELECT RequestId,@Tech,'2026-01-10T13:00:00',N'T09 SQL',2 FROM @Requests WHERE Label=N'old-pending';
    DELETE @TResult;
    INSERT @TResult EXEC dbo.usp_GetTechniciansWorkload @AsOfUtc='2026-01-11';
    INSERT @Results SELECT N'T08 multiple visits do not multiply assigned count',IIF(AssignedRequests=1 AND TotalWorkload=2,1,0),1 FROM @TResult WHERE TechnicianId=@Tech;
    -- Table variables retain the observations when the fixture transaction is rolled back.
    ROLLBACK;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
SELECT TestName,Passed,CASE WHEN Passed=1 THEN N'PASS' WHEN KnownDebt=1 THEN N'KNOWN SQL DEBT - SPECIFICATION FAIL' ELSE N'FAIL' END Result
FROM @Results ORDER BY TestName;
IF EXISTS(SELECT 1 FROM @Results WHERE Passed=0)
    THROW 51910, 'Report specification checks failed. Manual procedures were preserved by explicit user instruction; see result rows.', 1;
