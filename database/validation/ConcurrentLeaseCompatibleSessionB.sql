SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @UnitId INT = (SELECT u.UnitId FROM dbo.Units AS u INNER JOIN dbo.Properties AS p ON p.PropertyId = u.PropertyId WHERE p.Name = N'T02 Concurrency Fixture' AND u.Number = N'CON-001');
DECLARE @TenantId INT = (SELECT TenantId FROM dbo.Tenants WHERE Email = N't02.concurrency.b@example.test');

BEGIN TRANSACTION;
INSERT INTO dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
VALUES (@UnitId, @TenantId, '2041-02-01', '2041-02-28', 1000.00);
COMMIT TRANSACTION;
SELECT N'SESSION B: compatible lease committed.' AS ValidationResult;
