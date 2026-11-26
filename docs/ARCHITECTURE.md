# Arquitectura aprobada

## Registro de visitas T07

VisitService específico conserva el servicio de solicitudes enfocado en creación/transiciones. Controller delega; VisitRepository añade únicamente la visita y hace SaveChangesAsync. VisitModel y VisitMapping separan persistencia y negocio; VisitResponse sólo expone campos aprobados. Sin nuevos paquetes, esquema ni abstracción transaccional genérica.

VisitService administra TransactionScope ReadCommitted, async y timeout 30s. Antes de leer/validar estado adquiere LockForUpdateAsync sobre la solicitud (SELECT parametrizado UPDLOCK,HOLDLOCK). Después verifica técnico mediante el repositorio existente HOLDLOCK, captura UTC y añade la visita. Los bloqueos sobreviven hasta commit o rollback. Esto impide cancelar/cerrar o desactivar al técnico entre validación e inserción.

Coordinación T06: tras su lectura original, ChangeStatusAsync toma el mismo bloqueo padre antes de acceder a técnico/visitas. Conserva su RowVersion original; nunca la reemplaza por la actual. Orden común: solicitud padre → técnico si corresponde → visitas. Así se evita la inversión padre/hijos identificada en T06. Si una transición confirma primero, la visita lee el estado terminal y falla; si la visita confirma primero, la transición continúa después y esa visita es válida. Insertar Visits por sí mismo no modifica la RowVersion del padre; el protocolo no depende de que lo haga.

El protocolo aplica a estos servicios, no a escritores SQL que deliberadamente omitan las validaciones. Las transacciones siguen siendo cortas y sin retry automático. El handler diferencia conflictType ROW_VERSION, DEADLOCK y LOCK_TIMEOUT para los conflictos reconocidos; no clasifica todas las excepciones SQL como 409. Las pruebas concurrentes observan sys.dm_exec_requests para confirmar bloqueo real entre SPIDs distintos; requieren permisos diagnósticos sólo en la base/servidor local de pruebas.

Notes es NVARCHAR(MAX) nullable en SQL; la obligatoriedad y límite 2000 son del contrato aprobado, sin cambiar el esquema. Cost se valida en DTO/servicio y se persiste exactamente como decimal(18,2); el converter está limitado al campo Cost de entrada y no altera la serialización de dinero del resto de la API. Se conserva OpenAPI 3.1 y el transformer int32 de rutas para Swagger.

## Estados y concurrencia T06

Las cinco reglas de transición se centralizan en MaintenanceRequestModel.CanTransition mediante switch; DTO y servicio validan el contrato de asignación. MaintenanceRequestService administra una TransactionScope ReadCommitted (async, 30 segundos) que engloba lectura, validaciones y guardado. La lectura de solicitud es AsNoTracking y no conserva bloqueo compartido después de leer: permite una actualización concurrente y la detecta mediante RowVersion. No se aplicó Serializable a toda la operación.

TechnicianRepository.GetActiveStatusAsync usa consulta parametrizada con HOLDLOCK sólo sobre el técnico consultado para mantener estable su existencia/actividad hasta el commit. HasVisitsAsync usa EXISTS/AnyAsync y HOLDLOCK sobre las visitas de la solicitud, evitando que desaparezca la evidencia de cierre antes de confirmar; no carga colecciones. Las transacciones se mantienen cortas.

UpdateStatusAsync adjunta una entidad con los campos del modelo leído, marca únicamente StatusId/TechnicianId/ClosedAt y fija expresamente OriginalValue de RowVersion. No vuelve a leer una versión más reciente. SQL compara RequestId y RowVersion; cero filas produce DbUpdateConcurrencyException, que el handler traduce a 409 CONCURRENCY_CONFLICT. No hay reintentos automáticos. Un deadlock o lock timeout es un conflicto distinto (CONCURRENT_CHANGE), no evidencia de control optimista.

### Coordinación requerida para T07

Insertar Visits no cambia RowVersion de MaintenanceRequests. T07 deberá tomar un bloqueo de actualización sobre la fila padre antes de validar Assigned/InProgress y mantenerlo hasta insertar y confirmar la visita. La actualización terminal de T06 debe competir por esa misma fila: si confirmó primero, T07 debe leer el estado terminal y rechazar; si la visita bloqueó primero, la transición espera a que termine. No basta leer el estado y luego insertar sin coordinación. Debe conservarse orden de bloqueos coherente (padre antes de visitas) y tratar posibles deadlocks sin reintentar reglas obsoletas; el HOLDLOCK de visitas de T06 puede participar en ese orden y deberá revisarse durante T07. Esta tarea documenta el protocolo pendiente, sin registrar visitas mediante la API ni prometer protección para escritores que omitan ese protocolo.

## Creación de solicitudes T05

Flujo: MaintenanceRequestsController → MaintenanceRequestService → repositorios específicos T04 y consultas Exists de Unit/Tenant → SQL Server. El Controller sólo recibe/valida DTO nativo y devuelve respuesta explícita; los modelos EF no forman contratos HTTP.

El servicio administra una TransactionScope nativa Serializable con flujo async y timeout de 30 segundos. Todos los repositorios resuelven el mismo DbContext Scoped y se enlistan en la transacción local. Las lecturas mantienen bloqueos compartidos/de rango sobre los contratos hasta completar la inserción. AddAsync conserva SaveChanges interno; el servicio confirma únicamente al terminar. Las excepciones desechan la transacción, incluso si SaveChanges ya devolvió. No se introdujo Unit of Work ni un ejecutor transaccional personalizado. Mantener un solo contexto/servidor dentro del scope; añadir otros recursos podría exigir una transacción distribuida, fuera de alcance.

Se captura TimeProvider.GetUtcNow una sola vez dentro de la operación; se trunca a milisegundos para evitar que datetime2(3) redondee hasta el día siguiente. Esa misma hora define la fecha del contrato y CreatedAt persistido (el default SQL permanece disponible para otros consumidores). RequestId, RowVersion y estado inicial siguen obteniéndose de SQL. El trigger de Leases permanece activo; sus applocks protegen superposición, mientras Serializable protege la lectura contra modificaciones concurrentes. Pueden ocurrir bloqueos/deadlocks; timeout de lock o víctima de deadlock devuelven 409 sin retry automático. Una edición posterior al commit no está prohibida por esta operación; no se agrega una nueva regla histórica de inmutabilidad.

IExceptionHandler central produce ProblemDetails seguros y registra errores inesperados con ILogger. ApiController genera ValidationProblemDetails para JSON/validación. Swagger UI usa exclusivamente el documento nativo AddOpenApi/MapOpenApi (sin un segundo generador). En Development: `/swagger/index.html` y `/openapi/v1.json`. HTTP local en puerto 8080; HTTPS/TLS de producción queda en la configuración de despliegue.

## Acceso a datos T04

PropertyMaintenanceDbContext se registra Scoped con AddDbContext y la misma clave de conexión de DbUp. DbUp termina antes de atender peticiones; EF sólo consulta/persiste, sin modificar el esquema. LeaseRepository usa AnyAsync con límites inclusivos. MaintenanceRequestRepository devuelve modelos de negocio separados mediante mapeo explícito y consultas AsNoTracking; no expone IQueryable ni entidades EF.

AddAsync guarda una solicitud y devuelve el modelo con identidad, fecha UTC, estado y RowVersion generados. En T04 el repositorio es responsable de SaveChanges para esa operación atómica. SaveChanges usa la transacción nativa EF; futuras operaciones compuestas deberán delimitar una transacción explícita desde su servicio con el contexto Scoped compartido. No se introduce Unit of Work ni repositorio genérico. La lectura por ID es sólo de consulta; la edición y transiciones quedan pendientes.

## Decisión

La solución usa un monolito ASP.NET Core en capas pragmáticas dentro de `PropertyMaintenance.Api`, acompañado por dos proyectos de pruebas. El flujo es:

```text
Controller -> Service -> Repository -> SQL Server
```

La decisión V2 sustituye la separación anterior en proyectos Domain/Application/Infrastructure. El cambio es deliberado: el alcance de la prueba es acotado, tres proyectos reducen coordinación y sobreabstracción, y la estructura interna conserva separación clara entre negocio, persistencia, transporte y mapeos.

## Proyectos

| Proyecto | Responsabilidad | Referencias |
|---|---|---|
| `PropertyMaintenance.Api` | API, servicios, modelos, persistencia, mapeos y excepciones | Ninguna de los proyectos propios |
| `PropertyMaintenance.UnitTests` | Pruebas unitarias de reglas y servicios | `PropertyMaintenance.Api` |
| `PropertyMaintenance.IntegrationTests` | Pruebas de integración de API y SQL Server | `PropertyMaintenance.Api` |

Todos los proyectos usan `net10.0`. No hay dependencias circulares.

## Organización interna de Api

```text
Controllers/
DTOs/
Business/Services/
Business/Models/
Business/Enums/
DataAccess/Entities/
DataAccess/Repositories/
DataAccess/Context/
DataAccess/Migrations/
Mappings/
Exceptions/
```

`DataAccess/Migrations` es el espacio de scripts versionados y soporte DbUp; no se utilizarán migraciones de EF Core.

## Inicialización de base de datos

`DatabaseMigrator` se registra como servicio de `Api` y se ejecuta antes de `app.Run()`. Los scripts de `database/migrations` se embeben en el ensamblado con nombres deterministas y DbUp los ordena por nombre. El journal `dbo.SchemaVersions` evita repetir scripts ya aplicados. La estrategia es una transacción por script: un fallo revierte el script actual, conserva el journal de scripts completados y aborta el arranque de la API.

La base de datos se aprovisiona separadamente. En Azure SQL o producción puede crearse previamente con permisos controlados y luego ejecutar la inicialización con una identidad autorizada para DDL.

## Persistencia y contratos

EF Core 10 se usará Database First cuando T01 implemente el esquema. SQL Server es la fuente de verdad. Las entidades de persistencia no se expondrán directamente: los servicios trabajarán con modelos de negocio y los mapeos serán explícitos.

No se incorporan repositorios genéricos, MediatR, CQRS, AutoMapper ni dependencias innecesarias.

## Fuera de T02

No se implementan seed, entidades EF, repositorios, endpoints ni stored procedures de reportes. El trigger de T02 protege exclusivamente la superposición de contratos; las reglas de aplicación restantes siguen pendientes.
