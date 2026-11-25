SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Fixed IDs are part of the schema constraints in migration 002.
DECLARE @Expected TABLE (Catalog SYSNAME, Id TINYINT, Code NVARCHAR(50), Name NVARCHAR(100));
INSERT INTO @Expected VALUES
('UnitTypes',1,N'Apartment',N'Apartment'),('UnitTypes',2,N'Commercial',N'Commercial'),
('TechnicianSpecialties',1,N'Plumbing',N'Plumbing'),('TechnicianSpecialties',2,N'Electrical',N'Electrical'),
('TechnicianSpecialties',3,N'General',N'General'),
('RequestPriorities',1,N'Low',N'Low'),('RequestPriorities',2,N'Medium',N'Medium'),
('RequestPriorities',3,N'High',N'High'),('RequestPriorities',4,N'Urgent',N'Urgent'),
('RequestStatuses',1,N'Open',N'Open'),('RequestStatuses',2,N'Assigned',N'Assigned'),
('RequestStatuses',3,N'InProgress',N'In Progress'),('RequestStatuses',4,N'Closed',N'Closed'),
('RequestStatuses',5,N'Cancelled',N'Cancelled');

-- Reject conflicting IDs, codes or display names before inserting anything.
IF EXISTS
(
    SELECT 1 FROM @Expected e
    JOIN
    (
        SELECT 'UnitTypes' Catalog, UnitTypeId Id, Code, Name FROM dbo.UnitTypes
        UNION ALL SELECT 'TechnicianSpecialties', SpecialtyId, Code, Name FROM dbo.TechnicianSpecialties
        UNION ALL SELECT 'RequestPriorities', PriorityId, Code, Name FROM dbo.RequestPriorities
        UNION ALL SELECT 'RequestStatuses', StatusId, Code, Name FROM dbo.RequestStatuses
    ) a ON a.Catalog = e.Catalog AND (a.Id = e.Id OR a.Code = e.Code)
    WHERE a.Id <> e.Id
       OR a.Code COLLATE Latin1_General_100_BIN2 <> e.Code COLLATE Latin1_General_100_BIN2
       OR a.Name COLLATE Latin1_General_100_BIN2 <> e.Name COLLATE Latin1_General_100_BIN2
)
    THROW 51100, N'Existing catalog mappings conflict with the fixed schema mappings.', 1;

INSERT dbo.UnitTypes (UnitTypeId, Code, Name)
SELECT Id, Code, Name FROM @Expected e WHERE Catalog = 'UnitTypes'
AND NOT EXISTS (SELECT 1 FROM dbo.UnitTypes a WHERE a.UnitTypeId = e.Id);
INSERT dbo.TechnicianSpecialties (SpecialtyId, Code, Name)
SELECT Id, Code, Name FROM @Expected e WHERE Catalog = 'TechnicianSpecialties'
AND NOT EXISTS (SELECT 1 FROM dbo.TechnicianSpecialties a WHERE a.SpecialtyId = e.Id);
INSERT dbo.RequestPriorities (PriorityId, Code, Name)
SELECT Id, Code, Name FROM @Expected e WHERE Catalog = 'RequestPriorities'
AND NOT EXISTS (SELECT 1 FROM dbo.RequestPriorities a WHERE a.PriorityId = e.Id);
INSERT dbo.RequestStatuses (StatusId, Code, Name)
SELECT Id, Code, Name FROM @Expected e WHERE Catalog = 'RequestStatuses'
AND NOT EXISTS (SELECT 1 FROM dbo.RequestStatuses a WHERE a.StatusId = e.Id);
