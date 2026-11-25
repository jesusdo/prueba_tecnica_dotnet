# Backlog de implementación

Las tareas están ordenadas por dependencia. T00 es la única tarea ejecutada en esta fase.

| ID | Tarea | Dependencias | Criterios de aceptación verificables | Estado |
|---|---|---|---|---|
| T00 | Inspección, documentación y esqueleto de solución V2 | Ninguna | Tres proyectos `net10.0`; referencias válidas; docs creados; restore/build/test ejecutados; sin lógica de negocio | Completada |
| T01 | Esquema SQL y scripts DbUp | T00 | Base recreable desde cero; tablas, FKs, checks, catálogos, trigger y scripts versionados validados | Pendiente |
| T02 | Seed reproducible e idempotente | T01 | Datos mínimos aprobados; segunda ejecución no duplica; invariantes de negocio preservadas | Pendiente |
| T03 | Contexto EF Database First y mapeos | T01 | Entidades de persistencia generadas desde SQL; contexto y mapeos explícitos; sin migraciones EF | Pendiente |
| T04 | Repositorios específicos | T03 | Repositorios parametrizados para propiedades, contratos, solicitudes, técnicos y visitas; pruebas de acceso | Pendiente |
| T05 | Servicios y reglas de solicitudes | T04 | RN01–RN12 implementadas y probadas mediante interfaz pública del servicio | Pendiente |
| T06 | Endpoints REST de escritura | T05 | Crear solicitud, registrar visita y cambiar estado con códigos HTTP y Problem Details consistentes | Pendiente |
| T07 | Endpoints REST de consulta | T04 | Unidades por propiedad, listado paginado y tenant history con filtros y costos correctos | Pendiente |
| T08 | Stored procedure Properties Summary | T01, T04 | Parámetros de fecha; edificios sin actividad; agregaciones sin multiplicación de filas; resultado validado | Pendiente |
| T09 | Stored procedure Technicians Workload | T01, T04 | Solo activos; carga, visitas de 30 días, costo histórico y orden aprobado | Pendiente |
| T10 | Integración, Swagger, logging y manejo global | T06, T07, T08, T09 | API documentada; Problem Details; logs útiles; pruebas de integración en entorno dev | Pendiente |
| T11 | Verificación final y documentación de ejecución | T02, T10 | Restore/build/test completos; README actualizado sin borrar enunciado; supuestos y resultados documentados | Pendiente |

## Definición de terminado transversal

Cada tarea debe conservar decisiones aprobadas, usar consultas parametrizadas, reportar verificaciones reales y no introducir dependencias innecesarias. Las pruebas deben cubrir los efectos observables de cada regla modificada.

## Evidencia T00

- `docker compose run --rm dev dotnet restore PropertyMaintenance.slnx`: código 0.
- `docker compose run --rm dev dotnet build PropertyMaintenance.slnx --no-restore`: código 0, 0 advertencias y 0 errores.
- `docker compose run --rm dev dotnet test PropertyMaintenance.slnx --no-restore`: código 0; ambos ensamblados fueron descubiertos, pero no contienen pruebas reales todavía.
