SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @InitialProperties INT = (SELECT COUNT(*) FROM dbo.Properties);
DECLARE @InitialUnits INT = (SELECT COUNT(*) FROM dbo.Units);
DECLARE @InitialTenants INT = (SELECT COUNT(*) FROM dbo.Tenants);
DECLARE @InitialLeases INT = (SELECT COUNT(*) FROM dbo.Leases);
DECLARE @InitialTechnicians INT = (SELECT COUNT(*) FROM dbo.Technicians);
DECLARE @InitialRequests INT = (SELECT COUNT(*) FROM dbo.MaintenanceRequests);
DECLARE @InitialVisits INT = (SELECT COUNT(*) FROM dbo.Visits);
DECLARE @BeforeUtc DATETIME2(3) = CONVERT(DATETIME2(3), SYSUTCDATETIME());

DECLARE @PropertyId INT;
DECLARE @UnitId INT;
DECLARE @TenantId INT;
DECLARE @TechnicianId INT;
DECLARE @RequestId INT;
DECLARE @AssignedRequestId INT;

BEGIN TRANSACTION;

INSERT INTO dbo.Properties (Name, Address, City)
VALUES (N'Constraint Fixture Property', N'1 Constraint Street', N'Fixture City');
SET @PropertyId = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.Units (PropertyId, Number, Floor, UnitTypeId)
VALUES (@PropertyId, N'FIX-001', 1, 1);
SET @UnitId = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.Tenants (FullName, Email, Phone)
VALUES (N'Constraint Fixture Tenant', N'constraint.fixture@example.test', N'555-0100');
SET @TenantId = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.Technicians (Name, SpecialtyId, IsActive)
VALUES (N'Constraint Fixture Technician', 3, 1);
SET @TechnicianId = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
VALUES (@UnitId, @TenantId, '2025-01-01', '2025-12-31', 1000.00);

-- Commit fixture setup so each rejected statement is isolated in autocommit
-- mode. Some SQL Server constraint errors can doom an enclosing transaction.
COMMIT TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Leases WHERE UnitId = @UnitId AND TenantId = @TenantId AND MonthlyRent = 1000.00)
    THROW 51100, N'Constraint validation failed: valid lease was rejected.', 1;
SELECT N'PASS: valid lease accepted.' AS ValidationResult;

DECLARE @ErrorObserved BIT;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.Units (PropertyId, Number, Floor, UnitTypeId)
    VALUES (@PropertyId, N'FIX-001', 2, 1);
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51101, N'Constraint validation failed: duplicate unit number was accepted.', 1;
SELECT N'PASS: duplicate unit number rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.Tenants (FullName, Email, Phone)
    VALUES (N'Duplicate Email', N'constraint.fixture@example.test', N'555-0101');
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51102, N'Constraint validation failed: duplicate tenant email was accepted.', 1;
SELECT N'PASS: duplicate tenant email rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId, '2026-01-01', '2026-01-31', -1.00);
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51103, N'Constraint validation failed: negative monthly rent was accepted.', 1;
SELECT N'PASS: negative monthly rent rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId, '2026-02-01', '2026-01-31', 1000.00);
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51104, N'Constraint validation failed: lease with EndDate before StartDate was accepted.', 1;
SELECT N'PASS: invalid lease date range rejected.' AS ValidationResult;

INSERT INTO dbo.MaintenanceRequests
    (UnitId, TenantId, Category, PriorityId, Description)
VALUES
    (@UnitId, @TenantId, N'Fixture', 2, N'Default status fixture');
SET @RequestId = CONVERT(INT, SCOPE_IDENTITY());

DECLARE @AfterUtc DATETIME2(3) = CONVERT(DATETIME2(3), SYSUTCDATETIME());
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.MaintenanceRequests
    WHERE RequestId = @RequestId
      AND StatusId = 1
      AND ClosedAt IS NULL
      AND CreatedAt BETWEEN @BeforeUtc AND @AfterUtc
      AND DATALENGTH(RowVersion) = 8
)
    THROW 51105, N'Constraint validation failed: request defaults or rowversion are incorrect.', 1;
SELECT N'PASS: request defaults, UTC CreatedAt and RowVersion verified.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.MaintenanceRequests
        (UnitId, TenantId, TechnicianId, Category, PriorityId, Description)
    VALUES
        (@UnitId, @TenantId, @TechnicianId, N'Fixture', 2, N'Open request with technician');
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51106, N'Constraint validation failed: Open request with technician was accepted.', 1;
SELECT N'PASS: Open request with technician rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.MaintenanceRequests
        (UnitId, TenantId, Category, PriorityId, StatusId, Description, ClosedAt)
    VALUES
        (@UnitId, @TenantId, N'Fixture', 2, 1, N'Open request with close time', SYSUTCDATETIME());
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51107, N'Constraint validation failed: non-terminal request with ClosedAt was accepted.', 1;
SELECT N'PASS: non-terminal request with ClosedAt rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.MaintenanceRequests
        (UnitId, TenantId, TechnicianId, Category, PriorityId, StatusId, Description, ClosedAt)
    VALUES
        (@UnitId, @TenantId, @TechnicianId, N'Fixture', 2, 4, N'Closed request without close time', NULL);
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51108, N'Constraint validation failed: Closed request without ClosedAt was accepted.', 1;
SELECT N'PASS: Closed request without ClosedAt rejected.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.MaintenanceRequests
        (UnitId, TenantId, Category, PriorityId, StatusId, Description, ClosedAt, CreatedAt)
    VALUES
        (@UnitId, @TenantId, N'Fixture', 2, 5, N'Cancelled request with invalid close time', '2025-01-01', '2025-02-01');
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51109, N'Constraint validation failed: ClosedAt before CreatedAt was accepted.', 1;
SELECT N'PASS: ClosedAt before CreatedAt rejected.' AS ValidationResult;

INSERT INTO dbo.MaintenanceRequests
    (UnitId, TenantId, TechnicianId, Category, PriorityId, StatusId, Description)
VALUES
    (@UnitId, @TenantId, @TechnicianId, N'Fixture', 2, 2, N'Assigned request fixture');
SET @AssignedRequestId = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.MaintenanceRequests
    (UnitId, TenantId, Category, PriorityId, StatusId, Description, ClosedAt)
VALUES
    (@UnitId, @TenantId, N'Fixture', 2, 5, N'Cancelled request fixture', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE RequestId = CONVERT(INT, SCOPE_IDENTITY()))
    THROW 51110, N'Constraint validation failed: valid Cancelled request was rejected.', 1;
SELECT N'PASS: Cancelled request without technician accepted.' AS ValidationResult;

SET @ErrorObserved = 0;
BEGIN TRY
    INSERT INTO dbo.Visits (RequestId, TechnicianId, VisitedAt, Notes, Cost)
    VALUES (@AssignedRequestId, @TechnicianId, SYSUTCDATETIME(), N'Invalid cost fixture', -0.01);
END TRY
BEGIN CATCH
    SET @ErrorObserved = 1;
END CATCH;
IF @ErrorObserved = 0 THROW 51111, N'Constraint validation failed: negative visit cost was accepted.', 1;
SELECT N'PASS: negative visit cost rejected.' AS ValidationResult;

INSERT INTO dbo.Visits (RequestId, TechnicianId, Notes, Cost)
VALUES (@AssignedRequestId, @TechnicianId, N'Valid visit fixture', 0.00);

IF NOT EXISTS (SELECT 1 FROM dbo.Visits WHERE RequestId = @AssignedRequestId AND Cost = 0.00)
    THROW 51112, N'Constraint validation failed: zero-cost visit was rejected.', 1;
SELECT N'PASS: zero-cost visit accepted.' AS ValidationResult;

BEGIN TRANSACTION;

DELETE FROM dbo.Visits WHERE RequestId IN (SELECT RequestId FROM dbo.MaintenanceRequests WHERE UnitId = @UnitId);
DELETE FROM dbo.MaintenanceRequests WHERE UnitId = @UnitId;
DELETE FROM dbo.Leases WHERE UnitId = @UnitId;
DELETE FROM dbo.Units WHERE UnitId = @UnitId;
DELETE FROM dbo.Properties WHERE PropertyId = @PropertyId;
DELETE FROM dbo.Tenants WHERE TenantId = @TenantId;
DELETE FROM dbo.Technicians WHERE TechnicianId = @TechnicianId;

COMMIT TRANSACTION;

IF @InitialProperties <> (SELECT COUNT(*) FROM dbo.Properties)
   OR @InitialUnits <> (SELECT COUNT(*) FROM dbo.Units)
   OR @InitialTenants <> (SELECT COUNT(*) FROM dbo.Tenants)
   OR @InitialLeases <> (SELECT COUNT(*) FROM dbo.Leases)
   OR @InitialTechnicians <> (SELECT COUNT(*) FROM dbo.Technicians)
   OR @InitialRequests <> (SELECT COUNT(*) FROM dbo.MaintenanceRequests)
   OR @InitialVisits <> (SELECT COUNT(*) FROM dbo.Visits)
    THROW 51113, N'Constraint validation failed: fixture rows were not fully rolled back.', 1;

SELECT N'PASS: all constraint fixtures rolled back; no business rows remain from validation.' AS ValidationResult;
