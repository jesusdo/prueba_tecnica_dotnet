SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedTables TABLE (TableName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedTables (TableName)
VALUES
    (N'UnitTypes'), (N'TechnicianSpecialties'), (N'RequestPriorities'), (N'RequestStatuses'),
    (N'Properties'), (N'Units'), (N'Tenants'), (N'Leases'), (N'Technicians'),
    (N'MaintenanceRequests'), (N'Visits');

-- Journals are infrastructure tables, not additional business/catalog tables.
-- DemoSeedVersions is optional when demonstration data is disabled.
DECLARE @AllowedJournals TABLE (TableName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @AllowedJournals VALUES (N'SchemaVersions'), (N'DemoSeedVersions');

IF EXISTS
(
    SELECT 1
    FROM @ExpectedTables AS expected
    WHERE OBJECT_ID(N'dbo.' + expected.TableName, N'U') IS NULL
)
    THROW 51000, N'Schema validation failed: one or more expected tables are missing.', 1;

IF EXISTS
(
    SELECT 1
    FROM sys.tables AS actual
    WHERE actual.schema_id = SCHEMA_ID(N'dbo')
      AND actual.name NOT IN (SELECT TableName FROM @ExpectedTables)
      AND actual.name NOT IN (SELECT TableName FROM @AllowedJournals)
)
    THROW 51001, N'Schema validation failed: unexpected user tables exist in dbo.', 1;

DECLARE @ExpectedColumns TABLE
(
    TableName SYSNAME NOT NULL,
    ColumnName SYSNAME NOT NULL,
    TypeName SYSNAME NOT NULL,
    IsNullable BIT NOT NULL,
    MaxLength SMALLINT NOT NULL,
    PrecisionValue TINYINT NOT NULL,
    ScaleValue TINYINT NOT NULL,
    PRIMARY KEY (TableName, ColumnName)
);

INSERT INTO @ExpectedColumns
    (TableName, ColumnName, TypeName, IsNullable, MaxLength, PrecisionValue, ScaleValue)
VALUES
    (N'UnitTypes', N'UnitTypeId', N'tinyint', 0, 1, 3, 0),
    (N'UnitTypes', N'Code', N'nvarchar', 0, 100, 0, 0),
    (N'UnitTypes', N'Name', N'nvarchar', 0, 200, 0, 0),
    (N'TechnicianSpecialties', N'SpecialtyId', N'tinyint', 0, 1, 3, 0),
    (N'TechnicianSpecialties', N'Code', N'nvarchar', 0, 100, 0, 0),
    (N'TechnicianSpecialties', N'Name', N'nvarchar', 0, 200, 0, 0),
    (N'RequestPriorities', N'PriorityId', N'tinyint', 0, 1, 3, 0),
    (N'RequestPriorities', N'Code', N'nvarchar', 0, 100, 0, 0),
    (N'RequestPriorities', N'Name', N'nvarchar', 0, 200, 0, 0),
    (N'RequestStatuses', N'StatusId', N'tinyint', 0, 1, 3, 0),
    (N'RequestStatuses', N'Code', N'nvarchar', 0, 100, 0, 0),
    (N'RequestStatuses', N'Name', N'nvarchar', 0, 200, 0, 0),
    (N'Properties', N'PropertyId', N'int', 0, 4, 10, 0),
    (N'Properties', N'Name', N'nvarchar', 0, 400, 0, 0),
    (N'Properties', N'Address', N'nvarchar', 0, 600, 0, 0),
    (N'Properties', N'City', N'nvarchar', 0, 200, 0, 0),
    (N'Units', N'UnitId', N'int', 0, 4, 10, 0),
    (N'Units', N'PropertyId', N'int', 0, 4, 10, 0),
    (N'Units', N'Number', N'nvarchar', 0, 100, 0, 0),
    (N'Units', N'Floor', N'smallint', 0, 2, 5, 0),
    (N'Units', N'UnitTypeId', N'tinyint', 0, 1, 3, 0),
    (N'Tenants', N'TenantId', N'int', 0, 4, 10, 0),
    (N'Tenants', N'FullName', N'nvarchar', 0, 400, 0, 0),
    (N'Tenants', N'Email', N'nvarchar', 0, 640, 0, 0),
    (N'Tenants', N'Phone', N'nvarchar', 0, 100, 0, 0),
    (N'Leases', N'LeaseId', N'int', 0, 4, 10, 0),
    (N'Leases', N'UnitId', N'int', 0, 4, 10, 0),
    (N'Leases', N'TenantId', N'int', 0, 4, 10, 0),
    (N'Leases', N'StartDate', N'date', 0, 3, 10, 0),
    (N'Leases', N'EndDate', N'date', 1, 3, 10, 0),
    (N'Leases', N'MonthlyRent', N'decimal', 0, 9, 18, 2),
    (N'Technicians', N'TechnicianId', N'int', 0, 4, 10, 0),
    (N'Technicians', N'Name', N'nvarchar', 0, 400, 0, 0),
    (N'Technicians', N'SpecialtyId', N'tinyint', 0, 1, 3, 0),
    (N'Technicians', N'IsActive', N'bit', 0, 1, 1, 0),
    (N'MaintenanceRequests', N'RequestId', N'int', 0, 4, 10, 0),
    (N'MaintenanceRequests', N'UnitId', N'int', 0, 4, 10, 0),
    (N'MaintenanceRequests', N'TenantId', N'int', 0, 4, 10, 0),
    (N'MaintenanceRequests', N'TechnicianId', N'int', 1, 4, 10, 0),
    (N'MaintenanceRequests', N'Category', N'nvarchar', 0, 200, 0, 0),
    (N'MaintenanceRequests', N'PriorityId', N'tinyint', 0, 1, 3, 0),
    (N'MaintenanceRequests', N'StatusId', N'tinyint', 0, 1, 3, 0),
    (N'MaintenanceRequests', N'Description', N'nvarchar', 0, -1, 0, 0),
    (N'MaintenanceRequests', N'CreatedAt', N'datetime2', 0, 7, 23, 3),
    (N'MaintenanceRequests', N'ClosedAt', N'datetime2', 1, 7, 23, 3),
    (N'MaintenanceRequests', N'RowVersion', N'timestamp', 0, 8, 0, 0),
    (N'Visits', N'VisitId', N'int', 0, 4, 10, 0),
    (N'Visits', N'RequestId', N'int', 0, 4, 10, 0),
    (N'Visits', N'TechnicianId', N'int', 0, 4, 10, 0),
    (N'Visits', N'VisitedAt', N'datetime2', 0, 7, 23, 3),
    (N'Visits', N'Notes', N'nvarchar', 1, -1, 0, 0),
    (N'Visits', N'Cost', N'decimal', 0, 9, 18, 2);

IF EXISTS
(
    SELECT 1
    FROM @ExpectedColumns AS expected
    LEFT JOIN sys.tables AS t
        ON t.schema_id = SCHEMA_ID(N'dbo') AND t.name = expected.TableName
    LEFT JOIN sys.columns AS c
        ON c.object_id = t.object_id AND c.name = expected.ColumnName
    LEFT JOIN sys.types AS ty
        ON ty.user_type_id = c.user_type_id
    WHERE c.column_id IS NULL
       OR ty.name <> expected.TypeName
       OR c.is_nullable <> expected.IsNullable
       OR c.max_length <> expected.MaxLength
       OR c.precision <> expected.PrecisionValue
       OR c.scale <> expected.ScaleValue
)
    THROW 51002, N'Schema validation failed: one or more columns do not match the approved definition.', 1;

IF EXISTS
(
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.tables AS t ON t.object_id = c.object_id
    WHERE t.schema_id = SCHEMA_ID(N'dbo')
      AND t.name IN (SELECT TableName FROM @ExpectedTables)
      AND NOT EXISTS
      (
          SELECT 1
          FROM @ExpectedColumns AS expected
          WHERE expected.TableName = t.name AND expected.ColumnName = c.name
      )
)
    THROW 51003, N'Schema validation failed: unexpected columns exist in an approved table.', 1;

DECLARE @ExpectedPrimaryKeys TABLE (ConstraintName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedPrimaryKeys VALUES
    (N'PK_UnitTypes'), (N'PK_TechnicianSpecialties'), (N'PK_RequestPriorities'), (N'PK_RequestStatuses'),
    (N'PK_Properties'), (N'PK_Units'), (N'PK_Tenants'), (N'PK_Leases'), (N'PK_Technicians'),
    (N'PK_MaintenanceRequests'), (N'PK_Visits');

IF EXISTS (SELECT 1 FROM @ExpectedPrimaryKeys p WHERE NOT EXISTS (SELECT 1 FROM sys.key_constraints k WHERE k.name = p.ConstraintName AND k.type = 'PK'))
    THROW 51004, N'Schema validation failed: one or more primary keys are missing.', 1;

DECLARE @ExpectedUniques TABLE (ConstraintName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedUniques VALUES
    (N'UQ_UnitTypes_Code'), (N'UQ_TechnicianSpecialties_Code'), (N'UQ_RequestPriorities_Code'),
    (N'UQ_RequestStatuses_Code'), (N'UQ_Units_PropertyId_Number'), (N'UQ_Tenants_Email');

IF EXISTS (SELECT 1 FROM @ExpectedUniques u WHERE NOT EXISTS (SELECT 1 FROM sys.key_constraints k WHERE k.name = u.ConstraintName AND k.type = 'UQ'))
    THROW 51005, N'Schema validation failed: one or more unique constraints are missing.', 1;

DECLARE @ExpectedForeignKeys TABLE (ConstraintName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedForeignKeys VALUES
    (N'FK_Units_Properties'), (N'FK_Units_UnitTypes'), (N'FK_Leases_Units'), (N'FK_Leases_Tenants'),
    (N'FK_Technicians_Specialties'), (N'FK_MaintenanceRequests_Units'),
    (N'FK_MaintenanceRequests_Tenants'), (N'FK_MaintenanceRequests_Technicians'),
    (N'FK_MaintenanceRequests_Priorities'), (N'FK_MaintenanceRequests_Statuses'),
    (N'FK_Visits_MaintenanceRequests'), (N'FK_Visits_Technicians');

IF EXISTS (SELECT 1 FROM @ExpectedForeignKeys f WHERE NOT EXISTS (SELECT 1 FROM sys.foreign_keys k WHERE k.name = f.ConstraintName))
    THROW 51006, N'Schema validation failed: one or more foreign keys are missing.', 1;

IF EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE delete_referential_action <> 0 OR update_referential_action <> 0
)
    THROW 51007, N'Schema validation failed: cascading foreign key actions are not allowed.', 1;

DECLARE @ExpectedChecks TABLE (ConstraintName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedChecks VALUES
    (N'CK_Units_Number_NotBlank'), (N'CK_Tenants_Email_NotBlank'),
    (N'CK_Leases_MonthlyRent_NonNegative'), (N'CK_Leases_EndDate_NotBeforeStartDate'),
    (N'CK_MaintenanceRequests_ClosedAt_NotBeforeCreatedAt'),
    (N'CK_MaintenanceRequests_TerminalStatusClosedAt'),
    (N'CK_MaintenanceRequests_TechnicianByStatus'), (N'CK_Visits_Cost_NonNegative');

IF EXISTS (SELECT 1 FROM @ExpectedChecks c WHERE NOT EXISTS (SELECT 1 FROM sys.check_constraints k WHERE k.name = c.ConstraintName))
    THROW 51008, N'Schema validation failed: one or more check constraints are missing.', 1;

DECLARE @ExpectedDefaults TABLE (ConstraintName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedDefaults VALUES
    (N'DF_Technicians_IsActive'), (N'DF_MaintenanceRequests_StatusId'),
    (N'DF_MaintenanceRequests_CreatedAt'), (N'DF_Visits_VisitedAt');

IF EXISTS (SELECT 1 FROM @ExpectedDefaults d WHERE NOT EXISTS (SELECT 1 FROM sys.default_constraints c WHERE c.name = d.ConstraintName))
    THROW 51009, N'Schema validation failed: one or more default constraints are missing.', 1;

DECLARE @ExpectedIndexes TABLE (IndexName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @ExpectedIndexes VALUES
    (N'IX_Leases_UnitId_StartDate_EndDate'), (N'IX_Leases_TenantId'),
    (N'IX_MaintenanceRequests_UnitId_CreatedAt'), (N'IX_MaintenanceRequests_TenantId'),
    (N'IX_MaintenanceRequests_TechnicianId_StatusId'),
    (N'IX_MaintenanceRequests_ClosedAt_StatusId'), (N'IX_Visits_RequestId'),
    (N'IX_Visits_TechnicianId_VisitedAt'), (N'IX_Visits_VisitedAt');

IF EXISTS
(
    SELECT 1
    FROM @ExpectedIndexes i
    WHERE NOT EXISTS
    (
        SELECT 1 FROM sys.indexes x
        WHERE x.name = i.IndexName
          AND x.object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo'))
    )
)
    THROW 51010, N'Schema validation failed: one or more approved indexes are missing.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE t.schema_id = SCHEMA_ID(N'dbo')
      AND t.name = N'MaintenanceRequests'
      AND c.name = N'RowVersion'
      AND ty.system_type_id = 189
      AND c.max_length = 8
      AND c.is_nullable = 0
)
    THROW 51011, N'Schema validation failed: MaintenanceRequests.RowVersion is not configured as rowversion.', 1;

SELECT N'PASS: approved tables, columns, keys, constraints, defaults, indexes and RowVersion are present.' AS ValidationResult;
