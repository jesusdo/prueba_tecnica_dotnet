SET NOCOUNT ON;
SET XACT_ABORT ON;

CREATE TABLE dbo.Properties
(
    PropertyId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Properties PRIMARY KEY CLUSTERED,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(300) NOT NULL,
    City NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Units
(
    UnitId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Units PRIMARY KEY CLUSTERED,
    PropertyId INT NOT NULL,
    Number NVARCHAR(50) NOT NULL,
    Floor SMALLINT NOT NULL,
    UnitTypeId TINYINT NOT NULL,
    CONSTRAINT UQ_Units_PropertyId_Number UNIQUE (PropertyId, Number),
    CONSTRAINT FK_Units_Properties FOREIGN KEY (PropertyId)
        REFERENCES dbo.Properties (PropertyId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_Units_UnitTypes FOREIGN KEY (UnitTypeId)
        REFERENCES dbo.UnitTypes (UnitTypeId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT CK_Units_Number_NotBlank CHECK (LEN(LTRIM(RTRIM(Number))) > 0)
);

CREATE TABLE dbo.Tenants
(
    TenantId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Tenants PRIMARY KEY CLUSTERED,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Tenants_Email UNIQUE (Email),
    CONSTRAINT CK_Tenants_Email_NotBlank CHECK (LEN(LTRIM(RTRIM(Email))) > 0)
);

CREATE TABLE dbo.Leases
(
    LeaseId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Leases PRIMARY KEY CLUSTERED,
    UnitId INT NOT NULL,
    TenantId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    MonthlyRent DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_Leases_Units FOREIGN KEY (UnitId)
        REFERENCES dbo.Units (UnitId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_Leases_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (TenantId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT CK_Leases_MonthlyRent_NonNegative CHECK (MonthlyRent >= 0),
    CONSTRAINT CK_Leases_EndDate_NotBeforeStartDate CHECK (EndDate IS NULL OR EndDate >= StartDate)
);

CREATE TABLE dbo.Technicians
(
    TechnicianId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Technicians PRIMARY KEY CLUSTERED,
    Name NVARCHAR(200) NOT NULL,
    SpecialtyId TINYINT NOT NULL,
    IsActive BIT NOT NULL
        CONSTRAINT DF_Technicians_IsActive DEFAULT (1),
    CONSTRAINT FK_Technicians_Specialties FOREIGN KEY (SpecialtyId)
        REFERENCES dbo.TechnicianSpecialties (SpecialtyId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);

CREATE TABLE dbo.MaintenanceRequests
(
    RequestId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_MaintenanceRequests PRIMARY KEY CLUSTERED,
    UnitId INT NOT NULL,
    TenantId INT NOT NULL,
    TechnicianId INT NULL,
    Category NVARCHAR(100) NOT NULL,
    PriorityId TINYINT NOT NULL,
    StatusId TINYINT NOT NULL
        CONSTRAINT DF_MaintenanceRequests_StatusId DEFAULT (1),
    Description NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL
        CONSTRAINT DF_MaintenanceRequests_CreatedAt DEFAULT (CONVERT(DATETIME2(3), SYSUTCDATETIME())),
    ClosedAt DATETIME2(3) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_MaintenanceRequests_Units FOREIGN KEY (UnitId)
        REFERENCES dbo.Units (UnitId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_MaintenanceRequests_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (TenantId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_MaintenanceRequests_Technicians FOREIGN KEY (TechnicianId)
        REFERENCES dbo.Technicians (TechnicianId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_MaintenanceRequests_Priorities FOREIGN KEY (PriorityId)
        REFERENCES dbo.RequestPriorities (PriorityId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_MaintenanceRequests_Statuses FOREIGN KEY (StatusId)
        REFERENCES dbo.RequestStatuses (StatusId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT CK_MaintenanceRequests_ClosedAt_NotBeforeCreatedAt CHECK (ClosedAt IS NULL OR ClosedAt >= CreatedAt),
    CONSTRAINT CK_MaintenanceRequests_TerminalStatusClosedAt CHECK
    (
        (StatusId IN (4, 5) AND ClosedAt IS NOT NULL)
        OR (StatusId NOT IN (4, 5) AND ClosedAt IS NULL)
    ),
    CONSTRAINT CK_MaintenanceRequests_TechnicianByStatus CHECK
    (
        (StatusId = 1 AND TechnicianId IS NULL)
        OR (StatusId IN (2, 3, 4) AND TechnicianId IS NOT NULL)
        OR StatusId = 5
    )
);

CREATE TABLE dbo.Visits
(
    VisitId INT IDENTITY(1, 1) NOT NULL
        CONSTRAINT PK_Visits PRIMARY KEY CLUSTERED,
    RequestId INT NOT NULL,
    TechnicianId INT NOT NULL,
    VisitedAt DATETIME2(3) NOT NULL
        CONSTRAINT DF_Visits_VisitedAt DEFAULT (CONVERT(DATETIME2(3), SYSUTCDATETIME())),
    Notes NVARCHAR(MAX) NULL,
    Cost DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_Visits_MaintenanceRequests FOREIGN KEY (RequestId)
        REFERENCES dbo.MaintenanceRequests (RequestId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT FK_Visits_Technicians FOREIGN KEY (TechnicianId)
        REFERENCES dbo.Technicians (TechnicianId)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT CK_Visits_Cost_NonNegative CHECK (Cost >= 0)
);
