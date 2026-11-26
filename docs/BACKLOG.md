# Backlog de implementación

## Estado actual: T10.1 auditada, sin correcciones

Auditoría local sobre cfa7623 completada y documentada en TECHNICAL_AUDIT.md. Revalidación: 87 unitarias + 144 de integración aprobadas; build sin advertencias/errores, auditoría NuGet sin vulnerabilidades reportadas y publish Release exitoso. ValidateReports conserva seis PASS/nueve fallos (51910). Se reprodujeron fallos adicionales del validador de esquema tras seed, UPDATE válido por lote y rollback con Enlist=false. Catorce hallazgos consolidados: 0 P0, 5 P1, 7 P2, 2 P3. Ninguna corrección aplicada. T10 sigue pendiente de decisiones/correcciones; T10.2 y T11 no iniciadas. La revisión adicional Gentle AI de T09 fue rechazada por el responsable.

## T09: integración completada con deuda SQL aceptada

Por instrucción explícita se integran los SP manuales existentes sin modificarlos: dos GET, parámetros seguros, DTOs, DbUp 006/007 mediante recursos embebidos, pruebas SQL/HTTP y documentación. Los criterios originales de exactitud SQL no se declaran cumplidos: ValidateReports identifica las discrepancias y devuelve fallo verificable. Detalles en DECISIONES.md. T10 no iniciada.

## Estado al completar T08

Tres GET implementados: unidades con ocupación UTC inclusiva, solicitudes con filtros/paginación SQL y tenant history con costos exactos. Se reutilizan capas/fixtures existentes. Sin reportes, stored procedures, dependencias o cambios de migraciones. Pruebas verifican proyecciones y número acotado de consultas (2/2/3), comparaciones SQL independientes, límites extremos de fechas/offset y ausencia de duplicados entre páginas estables. Evidencia final y peticiones HTTP reales en DECISIONES.md. T09 permanece pendiente. Las notas de tareas anteriores conservan su contexto histórico.

## Alcance vigente T07

La instrucción T07 sustituye la antigua agrupación de endpoints de escritura: exclusivamente POST de visitas, validación de importes, coordinación concurrente con T06 y pruebas reales. T08 y posteriores permanecen pendientes; no se implementaron GET, paginación ni reportes.

## Alcance vigente T06

T06 implementa exclusivamente PATCH de estado: cinco transiciones, asignación, cierre con visita, cancelación, ProblemDetails, RowVersion original y pruebas de 25 pares/HTTP/concurrencia independiente. No registra visitas ni implementa T07. El protocolo requerido para T07 está documentado en ARCHITECTURE.md.

## Alcance vigente T05

La instrucción T05 del responsable sustituye la tarea anterior de repositorios completos: implementar únicamente creación de solicitudes, validación, servicio, POST, ProblemDetails, Swagger UI y pruebas. T06 y posteriores no se ejecutaron. La evidencia de pruebas y ejecución real se registra en DECISIONES.md.

## Estado al completar T04

T04 implementada: scaffold real de once tablas, EF Core/Design/dotnet-ef local 10.0.7, contexto Scoped, mapeo explícito, repositorios mínimos y pruebas SQL Server. Resultado real: 15 pruebas aprobadas, cero fallidas y cero omitidas en la ejecución final. T05 sigue pendiente; sus operaciones no incluidas en T04 no se implementaron.

## Estado al completar T03

T03 completada: migración 005 de catálogos, demo transaccional y opcional con journal independiente, 17 validaciones SQL aprobadas, segunda ejecución con datos íntegramente idénticos, caso sin demo y rollback ante fallo verificados en bases aisladas. Restore/build/test ejecutados en dev (sin pruebas C# reales todavía). T04 y posteriores pendientes. Este estado sustituye las notas históricas de tareas anteriores.

Las tareas están ordenadas por dependencia. T01 es la tarea ejecutada en esta fase; T02 y posteriores permanecen pendientes.

| ID | Tarea | Dependencias | Criterios de aceptación verificables | Estado |
|---|---|---|---|---|
| T00 | Inspección, documentación y esqueleto de solución V2 | Ninguna | Tres proyectos `net10.0`; referencias válidas; docs creados; restore/build/test ejecutados; sin lógica de negocio | Completada |
| T01 | Esquema SQL y validaciones | T00 | Once tablas; FKs restrictivas; checks, defaults, catálogos, índices y RowVersion; validaciones ejecutadas con rollback | Completada |
| T02 | DbUp y trigger de contratos superpuestos | T01 | DbUp con journal y scripts embebidos; trigger multirregistro y concurrente; validaciones y build exitosos | Completada |
| T03 | Seed reproducible e idempotente | T01, T02 | Datos mínimos aprobados; segunda ejecución no duplica; invariantes de negocio preservadas | Completada |
| T04 | Contexto EF Database First y mapeos | T01 | Entidades de persistencia generadas desde SQL; contexto y mapeos explícitos; sin migraciones EF | Completada |
| T05 | Crear solicitud de mantenimiento | T04 | POST 201 con contrato vigente, 400/404/409/500 ProblemDetails; transacción consistente; pruebas unitarias, HTTP y concurrentes; T04 sin regresiones | Completada |
| T06 | Transiciones de estado de solicitudes | T05 | Cinco de 25 pares permitidos; PATCH 200/400/404/409; técnico activo y visitas; RowVersion original; conflicto SQL real traducido a HTTP 409 | Completada |
| T07 | Registro de visitas | T06 | POST 201/400/404/409; importes exactos; estados/técnicos coherentes; concurrencia A/B/C real; flujo HTTP completo; suite previa sin regresiones | Completada |
| T08 | Endpoints REST de consulta | T04, T05, T06, T07 | Tres GET 200/400/404; vigencia UTC inclusiva; filtros antes de count/paginación SQL; orden determinista; costos sin duplicación; pruebas SQL/HTTP, fechas extremas y suite previa aprobadas | Completada |
| T09 | Integración de stored procedures manuales | T01, T05, T08 | Dos GET y DbUp verificados; parámetros/mapeos seguros; SQL preservado por orden expresa; discrepancias originales visibles en validación SQL | Integrada con deuda SQL aceptada |
| T10 | Integración, Swagger, logging y manejo global | T03, T07, T08, T09 | API documentada; Problem Details; logs útiles; pruebas de integración en entorno dev | En curso: T10.1 ejecutada; decisiones/correcciones pendientes |
| T10.1 | Auditoría técnica integral, sin correcciones | T00–T09 | Trazabilidad original, ejecuciones reales, hallazgos priorizados, evidencia SQL/concurrencia/seguridad y sólo informe/backlog modificados | Ejecutada; ver TECHNICAL_AUDIT.md |
| T10.2 | Correcciones que autorice el responsable tras la auditoría | T10.1 y aprobación de alcance | Resolver o aceptar hallazgos con evidencia; SQL manual reservado al desarrollador | No iniciada |
| T11 | Verificación final y documentación de ejecución | T03, T10 | Restore/build/test completos; README actualizado sin borrar enunciado; supuestos y resultados documentados | Pendiente |

## Definición de terminado transversal

Cada tarea debe conservar decisiones aprobadas, usar consultas parametrizadas, reportar verificaciones reales y no introducir dependencias innecesarias. Las pruebas deben cubrir los efectos observables de cada regla modificada.

## Evidencia T00

- `docker compose run --rm dev dotnet restore PropertyMaintenance.slnx`: código 0.
- `docker compose run --rm dev dotnet build PropertyMaintenance.slnx --no-restore`: código 0, 0 advertencias y 0 errores.
- `docker compose run --rm dev dotnet test PropertyMaintenance.slnx --no-restore`: código 0; ambos ensamblados fueron descubiertos, pero no contienen pruebas reales todavía.

## Evidencia T01

- `001_CreateCatalogTables.sql`, `002_CreateBusinessTables.sql` y `003_CreateIndexes.sql` se ejecutan en ese orden contra `PropertyMaintenanceDb`.
- `ValidateSchema.sql` comprueba tablas, columnas, tipos, nulabilidad, claves, FKs, checks, defaults, índices y `RowVersion`.
- `ValidateConstraints.sql` comprueba inserciones válidas, rechazos esperados y rollback completo de fixtures.
- Resultado real: 11 tablas, catálogos con 2/3/4/5 filas según corresponda, cero filas residuales de negocio, sin cascadas y sin triggers de objetos.
- `docker compose run --rm dev dotnet build PropertyMaintenance.slnx --no-restore`: código 0, 0 advertencias y 0 errores.
- `docker compose run --rm dev dotnet test PropertyMaintenance.slnx --no-restore`: código 0; no existen pruebas C# reales todavía.

## Evidencia T02

- DbUp 5.0.40 aplicó los cuatro scripts en una base limpia y registró cuatro filas en `dbo.SchemaVersions`.
- La segunda ejecución informó que no había migraciones pendientes y no reejecutó scripts.
- Una base aislada que falló en 004 conservó solo las tres migraciones aplicadas; tras corregir el batch, DbUp aplicó únicamente 004.
- `ValidateLeaseOverlap.sql` terminó con PASS en los 15 casos requeridos y eliminó sus fixtures.
- Tres pares de conexiones independientes probaron concurrencia: un solo ganador para el solapamiento, ambas compatibles confirmadas y ambas unidades diferentes confirmadas.
- `dotnet restore`, `dotnet build` y `dotnet test` terminaron con código 0; el test runner aún no contiene pruebas C# reales.
