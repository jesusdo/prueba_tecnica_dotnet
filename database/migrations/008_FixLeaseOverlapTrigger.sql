ALTER TRIGGER dbo.TR_Leases_PreventOverlap
ON dbo.Leases
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #AffectedUnits
    (
        UnitId INT NOT NULL PRIMARY KEY
    );

    INSERT INTO #AffectedUnits (UnitId)
    SELECT UnitId FROM inserted
    UNION
    SELECT UnitId FROM deleted;

    DECLARE @UnitId INT;
    DECLARE @LockResult INT;
    DECLARE @LockResource NVARCHAR(255);

    DECLARE affected_units CURSOR LOCAL FAST_FORWARD FOR
        SELECT UnitId
        FROM #AffectedUnits
        ORDER BY UnitId;

    OPEN affected_units;
    FETCH NEXT FROM affected_units INTO @UnitId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @LockResource = CONCAT(N'LeaseOverlap:Unit:', CONVERT(NVARCHAR(20), @UnitId));

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockResource,
            @LockMode = N'Exclusive',
            @LockOwner = N'Transaction',
            @LockTimeout = 10000,
            @DbPrincipal = N'public';

        IF @LockResult < 0
        BEGIN
            CLOSE affected_units;
            DEALLOCATE affected_units;
            THROW 51020, N'Could not acquire the lease overlap lock for the affected unit.', 1;
        END;

        FETCH NEXT FROM affected_units INTO @UnitId;
    END;

    CLOSE affected_units;
    DEALLOCATE affected_units;

    CREATE TABLE #NewLeases
    (
        InputRowId INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        LeaseId INT NOT NULL,
        UnitId INT NOT NULL,
        TenantId INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        MonthlyRent DECIMAL(18, 2) NOT NULL
    );

    INSERT INTO #NewLeases (LeaseId, UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    SELECT LeaseId, UnitId, TenantId, StartDate, EndDate, MonthlyRent
    FROM inserted;

    IF EXISTS
    (
        SELECT 1
        FROM #NewLeases AS newLease
        INNER JOIN dbo.Leases AS existingLease
            ON existingLease.UnitId = newLease.UnitId
           -- Updated rows are validated together below, not against their old versions.
           AND NOT EXISTS (SELECT 1 FROM deleted AS oldLease WHERE oldLease.LeaseId = existingLease.LeaseId)
           AND newLease.StartDate <= ISNULL(existingLease.EndDate, CONVERT(DATE, '99991231'))
           AND existingLease.StartDate <= ISNULL(newLease.EndDate, CONVERT(DATE, '99991231'))
    )
    BEGIN
        THROW 51021, N'Lease period overlaps another lease for the same unit.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM #NewLeases AS leftLease
        INNER JOIN #NewLeases AS rightLease
            ON rightLease.InputRowId > leftLease.InputRowId
           AND rightLease.UnitId = leftLease.UnitId
           AND leftLease.StartDate <= ISNULL(rightLease.EndDate, CONVERT(DATE, '99991231'))
           AND rightLease.StartDate <= ISNULL(leftLease.EndDate, CONVERT(DATE, '99991231'))
    )
    BEGIN
        THROW 51021, N'Lease period overlaps another lease for the same unit.', 1;
    END;

    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        UPDATE existingLease
        SET existingLease.UnitId = newLease.UnitId,
            existingLease.TenantId = newLease.TenantId,
            existingLease.StartDate = newLease.StartDate,
            existingLease.EndDate = newLease.EndDate,
            existingLease.MonthlyRent = newLease.MonthlyRent
        FROM dbo.Leases AS existingLease
        INNER JOIN #NewLeases AS newLease ON newLease.LeaseId = existingLease.LeaseId;
    END;
    ELSE
    BEGIN
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        SELECT UnitId, TenantId, StartDate, EndDate, MonthlyRent
        FROM #NewLeases;
    END;
END;
