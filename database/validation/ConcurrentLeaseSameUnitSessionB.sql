SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @UnitId INT =
(
    SELECT u.UnitId
    FROM dbo.Units AS u
    INNER JOIN dbo.Properties AS p ON p.PropertyId = u.PropertyId
    WHERE p.Name = N'T02 Concurrency Fixture' AND u.Number = N'CON-001'
);
DECLARE @TenantId INT = (SELECT TenantId FROM dbo.Tenants WHERE Email = N't02.concurrency.b@example.test');

BEGIN TRY
    BEGIN TRANSACTION;
    INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
    VALUES (@UnitId, @TenantId, '2040-06-01', '2040-06-30', 1000.00);
    COMMIT TRANSACTION;
    SELECT N'SESSION B: committed the same-unit lease.' AS ValidationResult;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 51021
        SELECT N'SESSION B: expected overlap rejection with error 51021.' AS ValidationResult;
    ELSE
        THROW;
END CATCH;
