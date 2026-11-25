SET NOCOUNT ON;
SET XACT_ABORT ON;

CREATE TABLE dbo.UnitTypes
(
    UnitTypeId TINYINT NOT NULL
        CONSTRAINT PK_UnitTypes PRIMARY KEY CLUSTERED,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_UnitTypes_Code UNIQUE (Code)
);

CREATE TABLE dbo.TechnicianSpecialties
(
    SpecialtyId TINYINT NOT NULL
        CONSTRAINT PK_TechnicianSpecialties PRIMARY KEY CLUSTERED,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_TechnicianSpecialties_Code UNIQUE (Code)
);

CREATE TABLE dbo.RequestPriorities
(
    PriorityId TINYINT NOT NULL
        CONSTRAINT PK_RequestPriorities PRIMARY KEY CLUSTERED,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_RequestPriorities_Code UNIQUE (Code)
);

CREATE TABLE dbo.RequestStatuses
(
    StatusId TINYINT NOT NULL
        CONSTRAINT PK_RequestStatuses PRIMARY KEY CLUSTERED,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_RequestStatuses_Code UNIQUE (Code)
);

INSERT INTO dbo.UnitTypes (UnitTypeId, Code, Name)
VALUES
    (1, N'Apartment', N'Apartment'),
    (2, N'Commercial', N'Commercial');

INSERT INTO dbo.TechnicianSpecialties (SpecialtyId, Code, Name)
VALUES
    (1, N'Plumbing', N'Plumbing'),
    (2, N'Electrical', N'Electrical'),
    (3, N'General', N'General');

INSERT INTO dbo.RequestPriorities (PriorityId, Code, Name)
VALUES
    (1, N'Low', N'Low'),
    (2, N'Medium', N'Medium'),
    (3, N'High', N'High'),
    (4, N'Urgent', N'Urgent');

INSERT INTO dbo.RequestStatuses (StatusId, Code, Name)
VALUES
    (1, N'Open', N'Open'),
    (2, N'Assigned', N'Assigned'),
    (3, N'InProgress', N'In Progress'),
    (4, N'Closed', N'Closed'),
    (5, N'Cancelled', N'Cancelled');
