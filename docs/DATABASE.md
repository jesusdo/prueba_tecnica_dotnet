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

## Supuestos de T01

- Se usan longitudes explícitas para que el esquema sea defendible en entrevista: nombres `NVARCHAR(200)`, direcciones `NVARCHAR(300)`, ciudades `NVARCHAR(100)`, números de unidad `NVARCHAR(50)`, correos `NVARCHAR(320)`, teléfonos `NVARCHAR(50)`, categorías `NVARCHAR(100)` y descripciones/notas `NVARCHAR(MAX)`.
- `Tenants.Phone` es obligatorio y `Visits.Notes` es opcional; el enunciado no definía nulabilidad y se eligió el mínimo consistente con el uso esperado.
- Los cuatro catálogos fijos se cargan en `001_CreateCatalogTables.sql` como datos de referencia necesarios para las FK; esto no constituye el seed definitivo de T03.
- Las reglas que dependen de otras filas o de la hora actual, como contratos superpuestos, técnico activo, existencia de visitas y estado de una solicitud al registrar una visita, quedan fuera de T01. El trigger de superposición corresponde a T02.

## Implementación T01

- `001_CreateCatalogTables.sql` crea los catálogos y sus valores fijos.
- `002_CreateBusinessTables.sql` crea las siete tablas de negocio, FKs restrictivas, defaults y checks estructurales.
- `003_CreateIndexes.sql` crea los nueve índices no redundantes; las restricciones únicas ya proveen los accesos únicos de unidad por edificio y email.
- `database/validation/ValidateSchema.sql` valida metadatos del esquema.
- `database/validation/ValidateConstraints.sql` prueba casos positivos y negativos dentro de una transacción que termina en rollback.

## Fuera de T01

T01 no implementa trigger de superposición, seed definitivo, DbUp, scaffold EF, entidades C#, repositorios, endpoints ni stored procedures.
