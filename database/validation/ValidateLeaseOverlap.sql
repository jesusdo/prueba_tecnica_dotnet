SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @FixturePropertyName NVARCHAR(200) = N'T02 Lease Overlap Fixture';
DECLARE @FixtureTenantEmail1 NVARCHAR(320) = N't02.lease.fixture.1@example.test';
DECLARE @FixtureTenantEmail2 NVARCHAR(320) = N't02.lease.fixture.2@example.test';
DECLARE @FixtureTenantEmail3 NVARCHAR(320) = N't02.lease.fixture.3@example.test';
DECLARE @FixturePropertyId INT;
DECLARE @UnitId INT;
DECLARE @SecondUnitId INT;
DECLARE @TenantId1 INT;
DECLARE @TenantId2 INT;
DECLARE @TenantId3 INT;
DECLARE @LeaseA INT;
DECLARE @LeaseB INT;
DECLARE @MovableLease INT;
DECLARE @ErrorNumber INT;
DECLARE @ErrorMessage NVARCHAR(2048);

BEGIN TRY
    SELECT @FixturePropertyId = PropertyId
    FROM dbo.Properties
    WHERE Name = @FixturePropertyName;

    IF @FixturePropertyId IS NOT NULL
    BEGIN
        DELETE leaseRow
        FROM dbo.Leases AS leaseRow
        INNER JOIN dbo.Units AS unitRow ON unitRow.UnitId = leaseRow.UnitId
        WHERE unitRow.PropertyId = @FixturePropertyId;

        DELETE FROM dbo.Units WHERE PropertyId = @FixturePropertyId;
        DELETE FROM dbo.Properties WHERE PropertyId = @FixturePropertyId;
    END;

    DELETE FROM dbo.Tenants
    WHERE Email IN (@FixtureTenantEmail1, @FixtureTenantEmail2, @FixtureTenantEmail3);

    INSERT INTO dbo.Properties (Name, Address, City)
    VALUES (@FixturePropertyName, N'T02 Fixture Address', N'T02 Fixture City');
    SET @FixturePropertyId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Units (PropertyId, Number, Floor, UnitTypeId)
    VALUES (@FixturePropertyId, N'T02-001', 1, 1);
    SET @UnitId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Units (PropertyId, Number, Floor, UnitTypeId)
    VALUES (@FixturePropertyId, N'T02-002', 1, 1);
    SET @SecondUnitId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Tenants (FullName, Email, Phone)
    VALUES
        (N'T02 Fixture Tenant 1', @FixtureTenantEmail1, N'555-0201'),
        (N'T02 Fixture Tenant 2', @FixtureTenantEmail2, N'555-0202'),
        (N'T02 Fixture Tenant 3', @FixtureTenantEmail3, N'555-0203');

    SELECT @TenantId1 = TenantId FROM dbo.Tenants WHERE Email = @FixtureTenantEmail1;
    SELECT @TenantId2 = TenantId FROM dbo.Tenants WHERE Email = @FixtureTenantEmail2;
    SELECT @TenantId3 = TenantId FROM dbo.Tenants WHERE Email = @FixtureTenantEmail3;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId1, '2025-01-01', '2025-01-31', 1000.00);
    SELECT @LeaseA = LeaseId
    FROM dbo.Leases
    WHERE UnitId = @UnitId AND TenantId = @TenantId1 AND StartDate = '2025-01-01';
    SELECT N'PASS 01: valid contract inserted.' AS ValidationResult;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId2, '2025-02-01', '2025-02-28', 1000.00);
    SELECT @LeaseB = LeaseId
    FROM dbo.Leases
    WHERE UnitId = @UnitId AND TenantId = @TenantId2 AND StartDate = '2025-02-01';
    SELECT N'PASS 02: later non-overlapping contract inserted.' AS ValidationResult;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId3, '2024-12-01', '2024-12-31', 1000.00);
    SELECT N'PASS 03: earlier non-overlapping contract inserted.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES (@UnitId, @TenantId3, '2025-01-31', '2025-02-10', 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51301, N'Case 04 failed: inclusive boundary overlap did not return the expected error.', 1;
    SELECT N'PASS 04: same-day boundary overlap rejected with error 51021.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES (@UnitId, @TenantId3, '2025-01-15', '2025-02-15', 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51302, N'Case 05 failed: partial overlap did not return the expected error.', 1;
    SELECT N'PASS 05: partial overlap rejected with error 51021.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES (@UnitId, @TenantId3, '2025-01-05', '2025-01-10', 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51303, N'Case 06 failed: contained overlap did not return the expected error.', 1;
    SELECT N'PASS 06: contained overlap rejected with error 51021.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES (@UnitId, @TenantId3, '2024-12-15', '2025-03-01', 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51304, N'Case 07 failed: containing overlap did not return the expected error.', 1;
    SELECT N'PASS 07: containing overlap rejected with error 51021.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES (@UnitId, @TenantId3, '2025-02-28', NULL, 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51305, N'Case 08 failed: open-ended overlap did not return the expected error.', 1;
    SELECT N'PASS 08: open-ended overlap rejected with error 51021.' AS ValidationResult;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@SecondUnitId, @TenantId1, '2025-01-01', '2025-01-31', 1000.00);
    SELECT N'PASS 09: simultaneous dates on a different unit accepted.' AS ValidationResult;

    UPDATE dbo.Leases
    SET MonthlyRent = 1100.00
    WHERE LeaseId = @LeaseA;
    SELECT N'PASS 10: update without changing lease dates accepted.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.Leases
        SET StartDate = '2025-01-15'
        WHERE LeaseId = @LeaseB;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51306, N'Case 11 failed: overlapping update did not return the expected error.', 1;
    SELECT N'PASS 11: date update creating overlap rejected with error 51021.' AS ValidationResult;

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
        VALUES
            (@UnitId, @TenantId1, '2030-01-01', '2030-01-31', 1000.00),
            (@UnitId, @TenantId2, '2030-01-15', '2030-02-15', 1000.00);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51307, N'Case 12 failed: incompatible multi-row insert did not return the expected error.', 1;
    SELECT N'PASS 12: incompatible multi-row insert rejected with error 51021.' AS ValidationResult;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES
        (@UnitId, @TenantId1, '2031-01-01', '2031-01-31', 1000.00),
        (@UnitId, @TenantId2, '2031-02-01', '2031-02-28', 1000.00);
    SELECT N'PASS 13: compatible multi-row insert accepted.' AS ValidationResult;

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId3, '2032-01-01', '2032-01-31', 1000.00);
    SELECT @MovableLease = LeaseId
    FROM dbo.Leases
    WHERE UnitId = @UnitId AND TenantId = @TenantId3 AND StartDate = '2032-01-01';

    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@SecondUnitId, @TenantId2, '2032-01-01', '2032-01-31', 1000.00);

    SET @ErrorNumber = NULL;
    SET @ErrorMessage = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.Leases
        SET UnitId = @SecondUnitId
        WHERE LeaseId = @MovableLease;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        SET @ErrorNumber = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;
    IF @ErrorNumber IS NULL OR @ErrorNumber <> 51021 OR @ErrorMessage IS NULL OR @ErrorMessage <> N'Lease period overlaps another lease for the same unit.'
        THROW 51308, N'Case 14 failed: UnitId update creating overlap did not return the expected error.', 1;
    SELECT N'PASS 14: UnitId update creating overlap rejected with error 51021.' AS ValidationResult;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Leases AS leftLease
        INNER JOIN dbo.Leases AS rightLease
            ON rightLease.UnitId = leftLease.UnitId
           AND rightLease.LeaseId > leftLease.LeaseId
           AND leftLease.StartDate <= ISNULL(rightLease.EndDate, CONVERT(DATE, '99991231'))
           AND rightLease.StartDate <= ISNULL(leftLease.EndDate, CONVERT(DATE, '99991231'))
        WHERE leftLease.UnitId IN (@UnitId, @SecondUnitId)
    )
        THROW 51309, N'Case 15 failed: invalid overlapping rows remain after validation.', 1;
    SELECT N'PASS 15: no invalid overlapping fixture rows remain.' AS ValidationResult;

    DELETE leaseRow
    FROM dbo.Leases AS leaseRow
    INNER JOIN dbo.Units AS unitRow ON unitRow.UnitId = leaseRow.UnitId
    WHERE unitRow.PropertyId = @FixturePropertyId;
    DELETE FROM dbo.Units WHERE PropertyId = @FixturePropertyId;
    DELETE FROM dbo.Properties WHERE PropertyId = @FixturePropertyId;
    DELETE FROM dbo.Tenants
    WHERE Email IN (@FixtureTenantEmail1, @FixtureTenantEmail2, @FixtureTenantEmail3);

    SELECT N'PASS: all lease overlap fixtures were removed.' AS ValidationResult;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

    DELETE leaseRow
    FROM dbo.Leases AS leaseRow
    INNER JOIN dbo.Units AS unitRow ON unitRow.UnitId = leaseRow.UnitId
    INNER JOIN dbo.Properties AS propertyRow ON propertyRow.PropertyId = unitRow.PropertyId
    WHERE propertyRow.Name = @FixturePropertyName;
    DELETE unitRow
    FROM dbo.Units AS unitRow
    INNER JOIN dbo.Properties AS propertyRow ON propertyRow.PropertyId = unitRow.PropertyId
    WHERE propertyRow.Name = @FixturePropertyName;
    DELETE FROM dbo.Properties WHERE Name = @FixturePropertyName;
    DELETE FROM dbo.Tenants
    WHERE Email IN (@FixtureTenantEmail1, @FixtureTenantEmail2, @FixtureTenantEmail3);

    THROW;
END CATCH;
