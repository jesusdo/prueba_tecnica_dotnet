# Backlog de implementación

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
| T07 | Endpoints REST de escritura | T06 | Crear solicitud, registrar visita y cambiar estado con códigos HTTP y Problem Details consistentes | Pendiente |
| T08 | Endpoints REST de consulta | T04, T05 | Unidades por propiedad, listado paginado y tenant history con filtros y costos correctos | Pendiente |
| T09 | Stored procedures de reportes | T01, T05 | Ambos reportes parametrizados; agregaciones correctas, edificios/técnicos sin actividad y orden aprobado | Pendiente |
| T10 | Integración, Swagger, logging y manejo global | T03, T07, T08, T09 | API documentada; Problem Details; logs útiles; pruebas de integración en entorno dev | Pendiente |
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
