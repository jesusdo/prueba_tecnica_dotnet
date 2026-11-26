# Diseño aprobado de base de datos

## T10.2: remediación autorizada

La migración nueva `008_FixLeaseOverlapTrigger.sql` altera el trigger existente sin editar 004 ni los SP manuales. La comparación contra filas persistidas excluye todos los LeaseId de `deleted`: son versiones anteriores reemplazadas por la sentencia, no contratos ajenos al lote. Las filas de `inserted` se comprueban entre sí y contra las persistidas no afectadas. El estado final debe seguir sin superposiciones; no se omite la detección entre miembros del mismo lote.

Se conservan límites inclusivos, EndDate NULL, error 51021, caminos INSERT/UPDATE y applocks exclusivos de propietario Transaction por la unión de UnitId anteriores/nuevos, adquiridos en orden ascendente. La corrección permite desplazar/intercambiar períodos o unidades válidamente. El orden reduce riesgos pero no garantiza ausencia general de deadlocks. EF conserva su configuración existente sin OUTPUT para Leases.

DbUp aplica ahora ocho recursos: 001–005, los SP canónicos embebidos como 006/007 y 008. Pruebas verifican base limpia y actualización real desde 001–007, journal de ocho filas, segunda aplicación sin cambios, definiciones de los SP idénticas antes/después y trigger corregido disponible. No se alteran recursos embebidos de los procedimientos ni migraciones históricas.

ValidateSchema distingue las once tablas aprobadas de los journals opcionales exactos dbo.SchemaVersions y dbo.DemoSeedVersions. No permite patrones de nombres ni ignora cualquier tabla adicional. Las comprobaciones de columnas/tipos, PK/FK/UQ/CHECK/DEFAULT, índices y RowVersion permanecen intactas. Regresiones prueban seed habilitado/deshabilitado, ausencia de Properties (51000) y tabla inesperada (51001), con rollback de DDL negativo.

ValidateReports conserva los nueve incumplimientos y el error 51910. Su archivo, los SP y la deuda de sus indicadores no se modifican. El estado y las evidencias de cierre están en la sección de remediación de TECHNICAL_AUDIT.md; las secciones posteriores conservan resultados históricos.

## T09: procedimientos manuales preservados

Ubicación canónica: database/scripts/dbo.usp_GetPropertiesSummary.sql y dbo.usp_GetTechniciansWorkload.sql. Autoría manual del desarrollador; revisión e integración/pruebas C# con apoyo de Codex. No se movieron, duplicaron ni editaron durante la integración.

El csproj los embebe como PropertyMaintenance.Api.database.migrations.006_CreatePropertiesSummaryProcedure.sql y 007_CreateTechniciansWorkloadProcedure.sql. DbUp aplica tras 001–005: siete registros reales en dbo.SchemaVersions. Se verificaron base limpia, actualización desde 001–005 y segunda ejecución sin cambios del journal. Una modificación posterior de un SP registrado requerirá nueva migración autorizada: editar el archivo canónico no lo vuelve a ejecutar automáticamente.

Metadatos reales: IDs/conteos INT; nombres NVARCHAR(200); AvgResolutionHours INT nullable; costos DECIMAL(38,2), nullable sólo en técnicos. El contrato de promedio decimal conserva exactamente el entero recibido, sin recuperar precisión perdida. Un importe fuera del rango de System.Decimal provoca error, sin pérdida silenciosa.

ValidateReports.sql utiliza fixtures con IDs generados dentro de una transacción y hace rollback. Requiere base aislada T04; muestra PASS o KNOWN SQL DEBT - SPECIFICATION FAIL y lanza 51910 si falla cualquier requisito. La deuda fue aceptada expresamente por el responsable, no corregida. Desde SSMS: conectar sólo a localhost, seleccionar PropertyMaintenanceDb_T04_Integration, abrir y ejecutar database/validation/ValidateReports.sql; revisar filas y error 51910. No usar la base principal.

Desde PowerShell (sustituir credencial local):

```powershell
Get-Content database/validation/ValidateReports.sql -Raw -Encoding utf8 |
  docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P <password-local> -C -b -d PropertyMaintenanceDb_T04_Integration
```

Ejemplos HTTP: /api/reports/properties-summary?from=2026-05-01&to=2026-11-26 y /api/reports/technicians-workload. Swagger: /swagger/index.html; documento: /openapi/v1.json.

## T04: EF Core Database First

EF Core SqlServer, Design y herramienta local dotnet-ef usan 10.0.7 estable. Design tiene PrivateAssets=all. El scaffold real se generó contra `PropertyMaintenanceDb_T03_Demo`, cuyo journal contenía 001–005. Se incluyeron exclusivamente las once tablas aprobadas; no los journals. El código generado se versiona en Entities y Context; personalizaciones sólo en `PropertyMaintenanceDbContext.Custom.cs`.

Desde la raíz dentro de dev, después de aprovisionar una base local e inicializarla mediante la API/DbUp, configurar `ConnectionStrings__PropertyMaintenance` por entorno. Dentro de Docker usar `Server=db,1433`; desde Windows `Server=localhost,1433`. Ejecutar:

```bash
dotnet tool restore
bash database/scaffold.sh
```

El script contiene el comando exacto scaffold y la lista explícita de tablas, namespaces, directorios y `--no-onconfiguring`. Usa la conexión del entorno sin guardarla en el código. Para regenerar: aplicar primero nuevos scripts DbUp; revisar el diff existente; identificar los doce archivos generados (once entidades y contexto); ejecutar `bash database/scaffold.sh --force` únicamente tras verificar que ningún archivo generado contiene personalizaciones. El archivo `.Custom.cs` no es salida del scaffold. Revisar el diff y ejecutar las pruebas. No ejecutar migraciones EF.

Mapeo: `date` a DateOnly/DateOnly?, datetime2(3) a DateTime/DateTime? con precisión 3, importes a decimal(18,2), tinyint a byte, smallint a short. SQL Server genera identidades, CreatedAt, estado inicial y RowVersion. RowVersion es byte[], generado al insertar/actualizar y token de concurrencia. Las navegaciones y nulabilidad proceden del esquema. Las FKs usan NoAction en la personalización para conservar restricciones al eliminar. Los IDs fijos de catálogos se conservan.

DATETIME2 no almacena zona: por contrato todos los eventos son UTC. EF materializa DateTime Unspecified; el mapeo al modelo de negocio establece Kind=Utc sin convertir la hora. Los contratos usan DateOnly y no sufren conversión de zona.

El scaffold detectó HasTrigger; además se declara UseSqlOutputClause(false) para Leases. La compatibilidad se verifica escribiendo y rechazando solapamientos con el trigger habilitado. La recuperación de IDENTITY en INSERT con un trigger INSTEAD OF debe verificarse específicamente antes de añadir operaciones de creación de contratos; T04 sólo expone consulta de contratos.

Pruebas: configurar `PROPERTY_MAINTENANCE_TEST_CONNECTION` con SQL Server local y una base cuyo nombre empiece por `PropertyMaintenanceDb_T04_`. Aprovisionarla separadamente mediante CREATE DATABASE. La fixture aplica DbUp/seed; nunca elimina la base principal. Ejecutar desde dev `dotnet test PropertyMaintenance.slnx`. Una configuración ausente produce fallo visible, no éxito simulado.

## T03: datos de demostración

Las migraciones obligatorias incluyen `005_SeedCatalogs.sql`: verifica ID, Code y Name y falla ante correspondencias incompatibles. No modifica 001–004. Los catálogos existen aunque se deshabilite la demostración.

`Database:SeedDemoData` es `false` por defecto en todos los entornos. Para habilitar: establecer `Database__SeedDemoData=true` y `ConnectionStrings__PropertyMaintenance` antes de iniciar la API. DbUp ejecuta el recurso embebido `database/seed/001_SeedDemoData.sql` después del esquema, con una transacción por script y journal independiente `dbo.DemoSeedVersions`. El journal y la carga confirman juntos; un fallo aborta el arranque. Nunca insertar manualmente entradas del journal.

La carga inicial exige las siete tablas de negocio vacías, tomando bloqueos de tabla durante la transacción. No sobrescribe ni elimina información. Las ejecuciones posteriores consultan el journal y no cambian fechas ni registros. No se soporta ejecutar inicializadores concurrentes; iniciar una sola instancia durante el aprovisionamiento.

Se captura una única referencia UTC a medianoche, persistida en `MaintenanceRequests.Description` como `T03|NNN|UTC=yyyy-MM-ddTHH:mm:ssZ|...`. Todas las fechas se derivan de ella. Las validaciones utilizan esa referencia, no la fecha del día: los datos históricos envejecen deliberadamente. Los contratos futuros son la única excepción a eventos sin fechas futuras. Los identificadores se resuelven por claves de fixtures, sin suponer valores IDENTITY.

Distribución: 3 edificios, 30 unidades (18 departamentos y 12 locales), 20 inquilinos, 26 contratos (18 vigentes, 6 vencidos, 2 futuros), 5 técnicos (4 activos y 1 inactivo), 100 solicitudes (15/20/20/35/10 por estado) y 110 visitas. Hay un técnico activo sin actividad, cancelaciones desde Open y Assigned, visitas gratuitas, múltiples visitas, técnicos diferentes al asignado y actividad histórica del técnico actualmente inactivo. El seed representa estados finales históricos; no ejecuta transiciones mediante la API.

### Ejecución local

Aprovisionar una base nueva identificada para pruebas; no reutilizar una base existente sin journal. Desde PowerShell:

```powershell
docker compose run --rm -e Database__SeedDemoData=true -e "ConnectionStrings__PropertyMaintenance=Server=db,1433;Database=PropertyMaintenanceDb_T03_Demo;User Id=sa;Password=Dev_Passw0rd_2026;TrustServerCertificate=True;" dev dotnet run --project src/PropertyMaintenance.Api
```

La contraseña mostrada es únicamente la credencial ficticia local de la plantilla. Usar la configuración propia si fue cambiada. Para iniciar sin datos ficticios, reemplazar `true` por `false` y seleccionar una base limpia distinta.

### Validación desde SSMS

1. Conectar al SQL Server local `localhost,1433` con la configuración de desarrollo.
2. Seleccionar explícitamente `PropertyMaintenanceDb_T03_Demo` en el selector de base.
3. Abrir y ejecutar completo `database/validation/ValidateSeed.sql`; esperar PASS 01–17. Cualquier incumplimiento produce THROW.
4. Consultar `SELECT * FROM dbo.SchemaVersions; SELECT * FROM dbo.DemoSeedVersions;`: cinco migraciones y un seed.
5. Reiniciar la API con la misma configuración; el journal debe seguir con una sola entrada de seed y los datos permanecer intactos.

Prueba reproducible de rollback: en otra base aislada ya migrada sin seed, crear temporalmente `CREATE TRIGGER dbo.T03_InjectSeedFailure ON dbo.Visits AFTER INSERT AS THROW 51999, N'T03 isolated seed failure probe.',1;` (en batch separado). Iniciar la API con seed habilitado. Debe abortar; comprobar cero filas en las siete tablas de negocio y ausencia de entrada exitosa en DemoSeedVersions. Retirar sólo ese trigger de prueba mediante `DROP TRIGGER dbo.T03_InjectSeedFailure;`. No alterar los scripts definitivos.

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

## DbUp y T02

- `DatabaseMigrator` ejecuta los recursos embebidos con DbUp y registra el journal en `dbo.SchemaVersions`.
- Las migraciones se ejecutan en orden numérico y con transacción por script. Una migración fallida detiene el arranque de la API.
- `ValidateSchema.sql` permite únicamente `dbo.SchemaVersions` como tabla adicional del journal; cualquier otra tabla inesperada sigue provocando fallo.
- `004_CreateLeaseOverlapTrigger.sql` crea `dbo.TR_Leases_PreventOverlap` para `INSERT` y `UPDATE`.
- El trigger adquiere `sp_getapplock` exclusivo, asociado a la transacción, para cada `UnitId` afectado y en orden ascendente. Luego valida el lote completo contra `dbo.Leases` usando intervalos inclusivos y sin `NOLOCK`.
- La adopción de una base T01 existente sin journal no se realiza automáticamente: primero se debe verificar o recrear la base en un entorno controlado. DbUp no marca scripts como ejecutados sin haberlos aplicado.

## Fuera de T02

T02 no implementa seed definitivo, scaffold EF, entidades C#, repositorios, endpoints ni stored procedures.

Nota T02: el trigger es `INSTEAD OF INSERT, UPDATE`; adquiere `sp_getapplock` exclusivo por `UnitId` afectado, en orden ascendente y antes de escribir en la tabla base. La validación de restricciones usa fixtures aislados y verifica su limpieza final.
