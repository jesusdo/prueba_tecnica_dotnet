SET NOCOUNT ON;
-- Read-only checks against the persisted fixture reference, never the validation clock.
DECLARE @Reference DATETIME2(3);
SELECT @Reference = MAX(TRY_CONVERT(DATETIME2(3), SUBSTRING(Description,13,19),126))
FROM dbo.MaintenanceRequests WHERE Description LIKE N'T03|___|UTC=%';
IF @Reference IS NULL OR (SELECT COUNT(*) FROM dbo.MaintenanceRequests
    WHERE Description LIKE N'T03|___|UTC=%'
      AND TRY_CONVERT(DATETIME2(3),SUBSTRING(Description,13,19),126) = @Reference
      AND SUBSTRING(Description,32,1) = N'Z') <> 100
    THROW 51201, N'01: All 100 requests must share one valid persisted UTC reference.', 1;
PRINT N'PASS 01: Persisted UTC reference';
DECLARE @Today DATE = CONVERT(DATE,@Reference);

IF (SELECT COUNT(*) FROM dbo.Properties) <> 3
 OR (SELECT COUNT(*) FROM dbo.Units) <> 30
 OR EXISTS (SELECT p.PropertyId FROM dbo.Properties p LEFT JOIN dbo.Units u ON u.PropertyId=p.PropertyId
            GROUP BY p.PropertyId HAVING COUNT(u.UnitId) <> 10)
    THROW 51202, N'02: Expected three properties with ten units each.', 1;
PRINT N'PASS 02: Properties and unit distribution';

IF (SELECT COUNT(*) FROM dbo.Units u JOIN dbo.UnitTypes t ON t.UnitTypeId=u.UnitTypeId WHERE t.Code=N'Apartment') <> 18
 OR (SELECT COUNT(*) FROM dbo.Units u JOIN dbo.UnitTypes t ON t.UnitTypeId=u.UnitTypeId WHERE t.Code=N'Commercial') <> 12
    THROW 51203, N'03: Expected 18 apartments and 12 commercial units.', 1;
PRINT N'PASS 03: Unit types';

IF (SELECT COUNT(*) FROM dbo.Tenants) <> 20
 OR EXISTS (SELECT Email FROM dbo.Tenants GROUP BY Email HAVING COUNT(*) > 1)
 OR EXISTS (SELECT 1 FROM dbo.Tenants t WHERE NOT EXISTS (SELECT 1 FROM dbo.Leases l WHERE l.TenantId=t.TenantId))
    THROW 51204, N'04: Expected 20 distinct tenants, all with leases.', 1;
PRINT N'PASS 04: Tenants';

IF (SELECT COUNT(*) FROM dbo.Leases) <> 26
 OR (SELECT COUNT(*) FROM dbo.Leases WHERE StartDate<=@Today AND (EndDate IS NULL OR EndDate>=@Today)) <> 18
 OR (SELECT COUNT(*) FROM dbo.Leases WHERE EndDate<@Today) <> 6
 OR (SELECT COUNT(*) FROM dbo.Leases WHERE StartDate>@Today) <> 2
    THROW 51205, N'05: Expected 18 current, six historical and two future leases.', 1;
PRINT N'PASS 05: Lease periods';

IF EXISTS (SELECT 1 FROM dbo.Leases a JOIN dbo.Leases b ON a.UnitId=b.UnitId AND a.LeaseId<b.LeaseId
    AND a.StartDate<=ISNULL(b.EndDate,CONVERT(DATE,'99991231'))
    AND b.StartDate<=ISNULL(a.EndDate,CONVERT(DATE,'99991231')))
 OR EXISTS (SELECT 1 FROM dbo.Leases WHERE MonthlyRent<0 OR EndDate<StartDate)
    THROW 51206, N'06: Lease intervals must be valid and never overlap, including endpoints.', 1;
PRINT N'PASS 06: Lease integrity';

IF (SELECT COUNT(*) FROM dbo.Technicians) <> 5
 OR (SELECT COUNT(*) FROM dbo.Technicians WHERE IsActive=1) <> 4
 OR (SELECT COUNT(*) FROM dbo.Technicians WHERE IsActive=0) <> 1
 OR NOT EXISTS (SELECT 1 FROM dbo.Technicians t WHERE IsActive=1
    AND NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests r WHERE r.TechnicianId=t.TechnicianId)
    AND NOT EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.TechnicianId=t.TechnicianId))
    THROW 51207, N'07: Expected four active technicians, one inactive and an active idle technician.', 1;
PRINT N'PASS 07: Technicians';

IF (SELECT COUNT(*) FROM dbo.MaintenanceRequests) <> 100
 OR EXISTS (SELECT 1 FROM (VALUES(1,15),(2,20),(3,20),(4,35),(5,10)) expected(StatusId,Total)
    WHERE (SELECT COUNT(*) FROM dbo.MaintenanceRequests r WHERE r.StatusId=expected.StatusId)<>expected.Total)
    THROW 51208, N'08: Request status totals must be 15/20/20/35/10.', 1;
PRINT N'PASS 08: Request states';

IF (SELECT COUNT(DISTINCT PriorityId) FROM dbo.MaintenanceRequests) <> 4
 OR EXISTS (SELECT 1 FROM dbo.RequestPriorities p WHERE p.PriorityId IN (1,2,3,4)
    AND NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests r WHERE r.PriorityId=p.PriorityId))
    THROW 51209, N'09: Requests must cover all four priorities.', 1;
PRINT N'PASS 09: Priorities';

IF EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE CreatedAt<DATEADD(MONTH,-6,@Reference) OR CreatedAt>@Reference)
 OR NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE CreatedAt>=DATEADD(DAY,-30,@Reference))
 OR NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE CreatedAt<DATEADD(DAY,-30,@Reference))
    THROW 51210, N'10: Requests must be within six months and on both sides of the 30-day window.', 1;
PRINT N'PASS 10: Request date windows';

IF EXISTS (SELECT 1 FROM dbo.MaintenanceRequests r WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Leases l WHERE l.UnitId=r.UnitId AND l.TenantId=r.TenantId
     AND l.StartDate<=CONVERT(DATE,r.CreatedAt) AND (l.EndDate IS NULL OR l.EndDate>=CONVERT(DATE,r.CreatedAt))))
    THROW 51211, N'11: Every reporter must have a lease covering the unit at request creation.', 1;
PRINT N'PASS 11: Reporter lease coverage';

IF EXISTS (SELECT 1 FROM dbo.MaintenanceRequests r LEFT JOIN dbo.Technicians t ON t.TechnicianId=r.TechnicianId
    WHERE (r.StatusId=1 AND r.TechnicianId IS NOT NULL)
       OR (r.StatusId IN (2,3,4) AND r.TechnicianId IS NULL)
       OR (r.StatusId IN (2,3) AND t.IsActive=0)
       OR (r.StatusId IN (4,5) AND (r.ClosedAt IS NULL OR r.ClosedAt<r.CreatedAt OR r.ClosedAt>@Reference))
       OR (r.StatusId NOT IN (4,5) AND r.ClosedAt IS NOT NULL))
    THROW 51212, N'12: Assignment and terminal dates must agree with request state.', 1;
PRINT N'PASS 12: Request state coherence';

IF EXISTS (SELECT 1 FROM dbo.MaintenanceRequests r WHERE r.StatusId=4
    AND NOT EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.RequestId=r.RequestId))
 OR EXISTS (SELECT 1 FROM dbo.Visits v JOIN dbo.MaintenanceRequests r ON r.RequestId=v.RequestId
    WHERE v.VisitedAt<r.CreatedAt OR v.VisitedAt>@Reference OR v.VisitedAt>r.ClosedAt OR r.StatusId IN (1,2,5))
    THROW 51213, N'13: Closed requests need visits, with coherent visit dates and states.', 1;
PRINT N'PASS 13: Visit chronology';

IF EXISTS (SELECT 1 FROM dbo.Visits WHERE Cost<0)
 OR NOT EXISTS (SELECT 1 FROM dbo.Visits WHERE Cost=0)
 OR NOT EXISTS (SELECT 1 FROM dbo.Visits WHERE Cost>0)
 OR NOT EXISTS (SELECT RequestId FROM dbo.Visits GROUP BY RequestId HAVING COUNT(*)>1 AND COUNT(DISTINCT TechnicianId)>1)
 OR NOT EXISTS (SELECT 1 FROM dbo.Visits v JOIN dbo.MaintenanceRequests r ON r.RequestId=v.RequestId WHERE v.TechnicianId<>r.TechnicianId)
    THROW 51214, N'14: Expected zero and paid visits, multiple visits and different technicians.', 1;
PRINT N'PASS 14: Visit costs and technician variety';

IF NOT EXISTS (SELECT 1 FROM dbo.Visits v JOIN dbo.Technicians t ON t.TechnicianId=v.TechnicianId
    JOIN dbo.MaintenanceRequests r ON r.RequestId=v.RequestId
    WHERE t.IsActive=0 AND r.StatusId=4 AND r.ClosedAt<DATEADD(DAY,-30,@Reference))
 OR EXISTS (SELECT 1 FROM dbo.Visits v JOIN dbo.Technicians t ON t.TechnicianId=v.TechnicianId
    JOIN dbo.MaintenanceRequests r ON r.RequestId=v.RequestId
    WHERE t.IsActive=0 AND (r.StatusId<>4 OR r.ClosedAt>=DATEADD(DAY,-30,@Reference)))
    THROW 51215, N'15: Inactive technician visits must exist only in historical closed work.', 1;
PRINT N'PASS 15: Historical inactive technician';

IF (SELECT COUNT(DISTINCT UnitId) FROM dbo.Leases WHERE StartDate<=@Today AND (EndDate IS NULL OR EndDate>=@Today)) <> 18
 OR NOT EXISTS (SELECT 1 FROM dbo.Leases WHERE EndDate IS NULL)
 OR NOT EXISTS (SELECT 1 FROM dbo.Leases WHERE StartDate<=@Today AND EndDate>=@Today)
 OR NOT EXISTS (SELECT TenantId FROM dbo.Leases GROUP BY TenantId HAVING COUNT(*)>1)
 OR NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name=N'TR_Leases_PreventOverlap' AND is_disabled=0)
    THROW 51216, N'16: Occupancy, lease variety or enabled trigger is incorrect.',1;
PRINT N'PASS 16: 18 occupied / 12 vacant, lease variety and enabled trigger';
IF NOT EXISTS (SELECT 1 FROM dbo.Visits WHERE VisitedAt>=DATEADD(DAY,-30,@Reference))
 OR NOT EXISTS (SELECT 1 FROM dbo.Visits WHERE VisitedAt<DATEADD(DAY,-30,@Reference))
 OR NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE StatusId=5 AND TechnicianId IS NULL)
 OR NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE StatusId=5 AND TechnicianId IS NOT NULL)
    THROW 51217,N'17: Visit windows or cancellation scenarios are missing.',1;
PRINT N'PASS 17: Visit windows and cancellation from Open/Assigned';
PRINT N'All 17 T03 seed checks passed against the persisted UTC fixture reference.';
