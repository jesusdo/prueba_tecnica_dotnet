SET NOCOUNT ON;

INSERT INTO dbo.Properties (Name, Address, City)
VALUES (N'T02 Concurrency Fixture', N'T02 Concurrency Address', N'T02 Concurrency City');

DECLARE @PropertyId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.Units (PropertyId, Number, Floor, UnitTypeId)
VALUES (@PropertyId, N'CON-001', 1, 1), (@PropertyId, N'CON-002', 1, 1);

INSERT INTO dbo.Tenants (FullName, Email, Phone)
VALUES
    (N'T02 Concurrency Tenant A', N't02.concurrency.a@example.test', N'555-0301'),
    (N'T02 Concurrency Tenant B', N't02.concurrency.b@example.test', N'555-0302');

SELECT N'Concurrency fixtures prepared. Run the paired SessionA and SessionB scripts from two independent connections.' AS Instruction;
