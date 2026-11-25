# Diseño aprobado de base de datos

## Parámetros generales

- Base: `PropertyMaintenanceDb`.
- Schema: `dbo`.
- SQL Server 2022.
- Tablas en plural, inglés y PascalCase.
- SQL versionado en `database/migrations` y aplicado mediante DbUp.
- No se usarán migraciones de EF Core.

## Tablas principales

| Tabla | Columnas aprobadas |
|---|---|
| `Properties` | `PropertyId`, `Name`, `Address`, `City` |
| `Units` | `UnitId`, `PropertyId`, `Number`, `Floor`, `UnitTypeId` |
| `Tenants` | `TenantId`, `FullName`, `Email`, `Phone` |
| `Leases` | `LeaseId`, `UnitId`, `TenantId`, `StartDate`, `EndDate`, `MonthlyRent` |
| `Technicians` | `TechnicianId`, `Name`, `SpecialtyId`, `IsActive` |
| `MaintenanceRequests` | `RequestId`, `UnitId`, `TenantId`, `TechnicianId`, `Category`, `PriorityId`, `StatusId`, `Description`, `CreatedAt`, `ClosedAt`, `RowVersion` |
| `Visits` | `VisitId`, `RequestId`, `TechnicianId`, `VisitedAt`, `Notes`, `Cost` |

## Catálogos fijos

Todos usan `TINYINT`, `Code` único y `Name`: `UnitTypes` (`Apartment=1`, `Commercial=2`), `TechnicianSpecialties` (`Plumbing=1`, `Electrical=2`, `General=3`), `RequestPriorities` (`Low=1`, `Medium=2`, `High=3`, `Urgent=4`) y `RequestStatuses` (`Open=1`, `Assigned=2`, `InProgress=3`, `Closed=4`, `Cancelled=5`).

## Tipos, integridad e índices

- PK principales: `INT IDENTITY`.
- Fechas contractuales: `DATE`, inclusivas; eventos UTC: `DATETIME2(3)`.
- Importes: `DECIMAL(18,2)`; textos: `NVARCHAR`; piso: `SMALLINT`; concurrencia: `ROWVERSION`.
- Email único y número de unidad único por edificio.
- FKs restrictivas, sin eliminación en cascada.
- CHECK constraints para importes no negativos, fechas y consistencia estructural.
- Trigger SQL para superposición de contratos en operaciones multirregistro y concurrentes.
- Índices candidatos: `Units(PropertyId, Number)`, `Tenants(Email)`, `Leases(UnitId, StartDate, EndDate)`, `Leases(TenantId)`, `MaintenanceRequests(UnitId, CreatedAt)`, `MaintenanceRequests(TenantId)`, `MaintenanceRequests(TechnicianId, StatusId)`, `MaintenanceRequests(ClosedAt, StatusId)`, `Visits(RequestId)`, `Visits(TechnicianId, VisitedAt)` y `Visits(VisitedAt)`. Se revisarán antes de implementarlos.

## Fuera de T00

T00 solo crea esta especificación y las carpetas reservadas. No crea tablas, scripts, triggers, índices, stored procedures ni seed.
