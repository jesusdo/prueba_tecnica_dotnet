# Requisitos aprobados

## Contrato implementado T05

`POST /api/maintenance-requests` recibe `unitId`, `tenantId`, `category`, `priority` y `description`. IDs positivos, categoría obligatoria de hasta 100 caracteres, descripción obligatoria de hasta 2000. Se recortan espacios exteriores al crear. Prioridades textuales Low/Medium/High/Urgent; no se aceptan números, valores desconocidos ni prioridad omitida. Los campos ajenos al DTO (incluidos los controlados por el servidor) se rechazan con 400.

La unidad y el inquilino deben existir y tener contrato válido en la misma unidad a la fecha UTC de creación, con ambos extremos inclusivos y fin nullable. Se crea Open, sin técnico ni fecha de cierre. Respuesta 201: requestId, unitId, tenantId, technicianId, category, priority, status, description, createdAt y closedAt. No incluye RowVersion ni Location inexistente.

Errores ProblemDetails: 400 validación/formato, 404 UNIT_NOT_FOUND o TENANT_NOT_FOUND, 409 ACTIVE_LEASE_REQUIRED o CONCURRENT_CHANGE y 500 UNEXPECTED_ERROR. El detalle interno nunca se devuelve al cliente. T05 no incluye cambios de estado, visitas, consultas ni reportes.

Este documento consolida el enunciado original de `README.md` y las decisiones aprobadas para la prueba. El README se conserva sin modificaciones como fuente original.

## Enunciado original preservado

El repositorio debe entregar una API de punta a punta para administrar edificios, unidades, inquilinos, contratos, técnicos, solicitudes de mantenimiento y visitas, incluyendo modelo de datos, base de datos, datos reproducibles y consultas que crucen varias tablas. La evaluación considera funcionamiento, modelo de datos y SQL, reglas de negocio, calidad de código y documentación.

El enunciado original exige C# y .NET 10, SQL Server 2022 o 2025 compatible con Azure SQL, una API REST documentada con OpenAPI/Swagger, esquema recreable, seed reproducible, manejo consistente de errores y documentación de ejecución. Los endpoints mínimos, extras y criterios completos permanecen en `README.md`.

## Alcance funcional aprobado

### Reglas de negocio

1. Una unidad no puede tener contratos con periodos superpuestos, incluidos contratos futuros e históricos.
2. Solo un inquilino con contrato vigente en la unidad puede crear una solicitud.
3. Las fechas contractuales son inclusivas. `EndDate` puede ser `NULL`.
4. Una solicitud nueva inicia en `Open`.
5. Las únicas transiciones son `Open -> Assigned`, `Open -> Cancelled`, `Assigned -> InProgress`, `Assigned -> Cancelled` e `InProgress -> Closed`.
6. Para asignar se requiere un técnico activo.
7. Para cerrar se requiere al menos una visita.
8. `Closed` y `Cancelled` registran `ClosedAt` en UTC.
9. El costo de una visita es mayor o igual a cero.
10. Solo se permiten visitas en `Assigned` e `InProgress`.
11. Cualquier técnico activo puede realizar una visita, aunque no sea el asignado.
12. No se permite reasignar técnicos ni reabrir solicitudes.

### Reportes

- `Properties Summary`: solicitudes abiertas creadas en el rango y actualmente activas; cerradas con `ClosedAt` en rango; promedio de resolución; costos de visitas en rango; edificios sin actividad; sin multiplicación de filas por joins.
- `Technicians Workload`: solo técnicos activos; solicitudes `Assigned` e `InProgress`; visitas de los últimos 30 días; costo histórico; técnicos sin actividad; orden por carga descendente y `TechnicianId`.
- Ambos reportes se implementarán mediante stored procedures parametrizados.

### Seed aprobado

Tres edificios, treinta unidades, veinte inquilinos, contratos vigentes e históricos, cinco técnicos con al menos uno inactivo y cien solicitudes de los últimos seis meses, con todos los estados representados y visitas coherentes. La ejecución debe ser reproducible e idempotente.

## Alcance de T00 V2

- Crear tres proyectos: `src/PropertyMaintenance.Api`, `tests/PropertyMaintenance.UnitTests` y `tests/PropertyMaintenance.IntegrationTests`.
- Todos deben apuntar a `net10.0`; ambos proyectos de pruebas referencian `Api`.
- Crear documentación, estructura de carpetas y solución.
- No crear tablas SQL, entidades de negocio, endpoints de negocio, seed, stored procedures, scaffold ni paquetes de aplicación no necesarios.
