SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Run only through DbUp: DemoSeedVersions owns repeatability and the transaction.
-- No identity values are assumed, including those generated inside the lease trigger.
IF @@TRANCOUNT = 0
    THROW 51101, N'Demo seed requires the transaction supplied by DbUp.', 1;

-- Hold table locks until DbUp completes, excluding concurrent business inserts.
IF EXISTS (SELECT 1 FROM dbo.Properties WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.Units WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.Tenants WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.Leases WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.Technicians WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WITH (TABLOCKX, HOLDLOCK))
 OR EXISTS (SELECT 1 FROM dbo.Visits WITH (TABLOCKX, HOLDLOCK))
    THROW 51102, N'Demo seed requires all seven business tables to be empty.', 1;

-- Midnight UTC is captured once. Every description stores this exact ISO reference.
-- Fixture keys are stable; dates move together only when rebuilding an empty database.
DECLARE @Reference DATETIME2(3) = CONVERT(DATE, SYSUTCDATETIME());
DECLARE @Today DATE = CONVERT(DATE, @Reference);
DECLARE @Numbers TABLE (N INT PRIMARY KEY);
INSERT @Numbers SELECT tens.N * 10 + ones.N + 1
FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) tens(N)
CROSS JOIN (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) ones(N);

INSERT dbo.Properties (Name, Address, City)
SELECT CONCAT(N'T03 Property ', N), CONCAT(N, N' Demo Avenue'), N'Demo City'
FROM @Numbers WHERE N <= 3;

DECLARE @Units TABLE (N INT PRIMARY KEY, UnitId INT NOT NULL);
INSERT dbo.Units (PropertyId, Number, Floor, UnitTypeId)
SELECT p.PropertyId, CONCAT(N'T03-', n.N), (n.N - 1) % 10 / 2,
       CASE WHEN (n.N - 1) % 10 < 6 THEN 1 ELSE 2 END
FROM @Numbers n JOIN dbo.Properties p ON p.Name = CONCAT(N'T03 Property ', (n.N - 1) / 10 + 1)
WHERE n.N <= 30;
INSERT @Units SELECT n.N, u.UnitId FROM @Numbers n
JOIN dbo.Units u ON u.Number = CONCAT(N'T03-', n.N) WHERE n.N <= 30;

INSERT dbo.Tenants (FullName, Email, Phone)
SELECT CONCAT(N'Demo Tenant ', N), CONCAT(N't03.tenant.', N, N'@example.test'), CONCAT(N'+1-555-010-', RIGHT(CONCAT(N'00', N), 2))
FROM @Numbers WHERE N <= 20;
DECLARE @Tenants TABLE (N INT PRIMARY KEY, TenantId INT NOT NULL);
INSERT @Tenants SELECT n.N, t.TenantId FROM @Numbers n
JOIN dbo.Tenants t ON t.Email = CONCAT(N't03.tenant.', n.N, N'@example.test') WHERE n.N <= 20;

-- Six past contracts precede the current contracts on units 1-6, inclusively disjoint.
INSERT dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
SELECT u.UnitId, t.TenantId, DATEADD(DAY,-500,@Today), DATEADD(DAY,-300,@Today), 900
FROM @Units u JOIN @Tenants t ON t.N = u.N WHERE u.N <= 6;
INSERT dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
SELECT u.UnitId, t.TenantId, DATEADD(DAY,-240,@Today),
       CASE WHEN u.N % 2 = 0 THEN DATEADD(DAY,365,@Today) END, 1000 + u.N * 25
FROM @Units u JOIN @Tenants t ON t.N = u.N WHERE u.N <= 18;
INSERT dbo.Leases (UnitId, TenantId, StartDate, EndDate, MonthlyRent)
SELECT u.UnitId, t.TenantId, DATEADD(DAY,30,@Today), DATEADD(DAY,395,@Today), 1500
FROM @Units u JOIN @Tenants t ON t.N = u.N WHERE u.N IN (19,20);

INSERT dbo.Technicians (Name, SpecialtyId, IsActive)
SELECT CONCAT(N'T03 Technician ', N), (N - 1) % 3 + 1, CASE WHEN N = 5 THEN 0 ELSE 1 END
FROM @Numbers WHERE N <= 5;
DECLARE @Techs TABLE (N INT PRIMARY KEY, TechnicianId INT NOT NULL);
INSERT @Techs SELECT n.N, t.TechnicianId FROM @Numbers n
JOIN dbo.Technicians t ON t.Name = CONCAT(N'T03 Technician ', n.N) WHERE n.N <= 5;

DECLARE @Requests TABLE (N INT PRIMARY KEY, Description NVARCHAR(200) NOT NULL);
INSERT @Requests SELECT N, CONCAT(N'T03|', RIGHT(CONCAT(N'000',N),3), N'|UTC=',
    CONVERT(NVARCHAR(23),@Reference,126), N'Z|Demo maintenance fixture') FROM @Numbers;

-- 15 Open, 20 Assigned, 20 InProgress, 35 Closed, 10 Cancelled.
-- Technician 4 is active and idle; technician 5 only appears in old completed work.
INSERT dbo.MaintenanceRequests
    (UnitId, TenantId, TechnicianId, Category, PriorityId, StatusId, Description, CreatedAt, ClosedAt)
SELECT u.UnitId, t.TenantId,
    CASE WHEN n.N <= 15 OR n.N > 95 THEN NULL
         WHEN n.N BETWEEN 56 AND 60 THEN inactiveTech.TechnicianId ELSE tech.TechnicianId END,
    CASE n.N % 3 WHEN 0 THEN N'Plumbing' WHEN 1 THEN N'Electrical' ELSE N'General' END,
    (n.N - 1) % 4 + 1,
    CASE WHEN n.N <= 15 THEN 1 WHEN n.N <= 35 THEN 2 WHEN n.N <= 55 THEN 3 WHEN n.N <= 90 THEN 4 ELSE 5 END,
    r.Description, DATEADD(DAY,-(1 + (n.N * 7) % 178),@Reference),
    CASE WHEN n.N > 55 THEN DATEADD(HOUR,12,DATEADD(DAY,-(1 + (n.N * 7) % 178),@Reference)) END
FROM @Numbers n
JOIN @Units u ON u.N = (n.N - 1) % 18 + 1
JOIN @Tenants t ON t.N = u.N
JOIN @Requests r ON r.N = n.N
JOIN @Techs tech ON tech.N = (n.N - 1) % 3 + 1
JOIN @Techs inactiveTech ON inactiveTech.N = 5;

-- Completed and in-progress work has two visits, with different technicians.
-- A zero-cost diagnostic visit is followed by a paid follow-up.
INSERT dbo.Visits (RequestId, TechnicianId, VisitedAt, Notes, Cost)
SELECT request.RequestId,
       CASE WHEN visit.N = 1 THEN request.TechnicianId ELSE otherTech.TechnicianId END,
       DATEADD(HOUR,visit.N * 3,request.CreatedAt),
       CASE WHEN visit.N = 1 THEN N'Diagnostic visit' ELSE N'Follow-up visit by another technician' END,
       CASE WHEN visit.N = 1 THEN 0 ELSE 50 + fixture.N END
FROM @Requests fixture
JOIN dbo.MaintenanceRequests request ON request.Description = fixture.Description
JOIN @Techs otherTech ON otherTech.N = fixture.N % 3 + 1
CROSS JOIN (VALUES(1),(2)) visit(N)
WHERE request.StatusId IN (3,4);

PRINT N'T03 seed prepared: 3 properties, 30 units, 20 tenants, 26 leases, 5 technicians, 100 requests, 110 visits. DbUp owns commit and journaling.';
