SET NOCOUNT ON;
SET XACT_ABORT ON;

-- UQ_Units_PropertyId_Number and UQ_Tenants_Email already provide the approved
-- unique access paths, so separate duplicate indexes are intentionally omitted.
CREATE INDEX IX_Leases_UnitId_StartDate_EndDate
    ON dbo.Leases (UnitId, StartDate, EndDate);

CREATE INDEX IX_Leases_TenantId
    ON dbo.Leases (TenantId);

CREATE INDEX IX_MaintenanceRequests_UnitId_CreatedAt
    ON dbo.MaintenanceRequests (UnitId, CreatedAt);

CREATE INDEX IX_MaintenanceRequests_TenantId
    ON dbo.MaintenanceRequests (TenantId);

CREATE INDEX IX_MaintenanceRequests_TechnicianId_StatusId
    ON dbo.MaintenanceRequests (TechnicianId, StatusId);

CREATE INDEX IX_MaintenanceRequests_ClosedAt_StatusId
    ON dbo.MaintenanceRequests (ClosedAt, StatusId);

CREATE INDEX IX_Visits_RequestId
    ON dbo.Visits (RequestId);

CREATE INDEX IX_Visits_TechnicianId_VisitedAt
    ON dbo.Visits (TechnicianId, VisitedAt);

CREATE INDEX IX_Visits_VisitedAt
    ON dbo.Visits (VisitedAt);
