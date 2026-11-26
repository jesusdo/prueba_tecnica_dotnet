# Requisitos aprobados

## Consultas implementadas T08

| GET | Respuesta 200 | Errores esperados |
|---|---|---|
| `/api/properties/{id}/units` | Arreglo de `unitId`, `number`, `floor`, `type` (Apartment/Commercial), `currentTenant` (`tenantId`, `fullName` o null) | 400 ID inválido; 404 PROPERTY_NOT_FOUND |
| `/api/maintenance-requests` | `page`, `pageSize`, `totalCount`, `totalPages`, `items` | 400 parámetros inválidos |
| `/api/tenants/{id}/history` | `tenantId`, `fullName`, `contracts`, `maintenanceRequests` | 400 ID inválido; 404 TENANT_NOT_FOUND |

Todos los IDs de ruta deben ser positivos. Colecciones vacías válidas retornan 200. Se reutilizan ProblemDetails y el handler global; errores inesperados retornan 500 sin detalles SQL.

- Unidades: todas las del edificio, orden `Number, UnitId`; inquilino vigente usando una fecha UTC capturada con TimeProvider, `StartDate <= fecha AND (EndDate IS NULL OR EndDate >= fecha)`. Se conservan unidades desocupadas; no se incluyen contratos históricos o futuros como ocupación actual.
- Listado: `page=1`, `pageSize=20` por defecto; page positivo, pageSize 1–100. Filtros combinables `status`, `priority`, `propertyId`, `from`, `to`. Los enums admiten exclusivamente nombres aprobados, sin distinguir mayúsculas: Open/Assigned/InProgress/Closed/Cancelled y Low/Medium/High/Urgent. Se rechazan números, valores vacíos y nombres desconocidos. propertyId debe ser positivo; si no existe devuelve colección vacía.
- Fechas de consulta: formato `yyyy-MM-dd`, límites inclusivos por día UTC sobre CreatedAt. from se convierte a medianoche UTC; to, a medianoche UTC del día siguiente exclusiva. Para `9999-12-31`, no se añade límite superior porque DATETIME2 no representa eventos posteriores. Rango invertido/formato inválido devuelve 400. El offset se calcula en long; si excede el conteo devuelve página vacía sin convertirlo a int ni desbordarse.
- Cada item del listado contiene `requestId`, `propertyId`, `propertyName`, `unitId`, `unitNumber`, `tenantId`, `tenantName`, `technicianId`, `technicianName`, `category`, `priority`, `status`, `description`, `createdAt`, `closedAt`. Técnico nullable, enums como texto, eventos UTC. Orden `CreatedAt DESC, RequestId DESC`; filtros antes del conteo y paginación. totalPages es cero sin resultados; página fuera de rango conserva metadatos y devuelve items vacío.
- Historial: contratos con `leaseId`, `propertyId`, `propertyName`, `unitId`, `unitNumber`, `startDate`, `endDate`, `monthlyRent`, `isActive`; incluye todos los períodos, orden `StartDate DESC, LeaseId DESC`, vigencia inclusiva calculada con TimeProvider/UTC.
- Solicitudes del historial se obtienen por TenantId, incluso después de vencer el contrato: `requestId`, `propertyId`, `propertyName`, `unitId`, `unitNumber`, `category`, `status`, `priority`, `createdAt`, `closedAt`, `totalVisitCost`. Orden `CreatedAt DESC, RequestId DESC`. Costo histórico de todas las visitas de cada solicitud, cero sin visitas, sin multiplicarlo por contratos.

Ejemplos sobre el seed: `/api/properties/2/units`, `/api/maintenance-requests?page=1&pageSize=2&status=Open&propertyId=1`, `/api/tenants/1/history`. Swagger UI: `/swagger/index.html`; documento: `/openapi/v1.json`. Las consultas usan offset: cambios concurrentes pueden desplazar filas entre páginas. No se garantiza una instantánea común entre consultas independientes. T09 no está implementada.

## Contrato implementado T07

`POST /api/maintenance-requests/{id}/visits` recibe technicianId positivo, notes obligatorias de hasta 2000 caracteres y cost numérico obligatorio (cero permitido). El ID de solicitud proviene exclusivamente de la ruta. Se rechazan VisitId, RequestId en el body, VisitedAt y demás miembros desconocidos. Notes se recorta en el servicio; no se permiten espacios solamente.

Cost usa decimal y debe pertenecer a DECIMAL(18,2): rango 0–9999999999999999.99 y escala máxima dos. Se verifica la escala efectiva del número JSON antes de parsearlo para evitar redondeos de entradas de precisión extrema. Notación científica representable como 1e-2 se admite; 1e-3 se rechaza. Una representación con más de dos decimales efectivos, incluso ceros adicionales, se rechaza explícitamente. Omitir/null no equivale a cero.

Sólo Assigned e InProgress permiten visitas. Solicitud y técnico deben existir; técnico activo, sin exigencia de coincidencia con el asignado ni de especialidad. Múltiples visitas permitidas sin cambiar estado o técnico de la solicitud. VisitedAt de TimeProvider UTC, truncado a milisegundos, nunca anterior a CreatedAt. Respuesta 201: visitId, requestId, technicianId, visitedAt, notes y cost, sin Location inexistente.

Errores ProblemDetails: 400 entrada/formato/importes inválidos; 404 REQUEST_NOT_FOUND o TECHNICIAN_NOT_FOUND; 409 INVALID_VISIT_STATUS, TECHNICIAN_INACTIVE o conflicto identificado; 500 inesperados. Una fecha de servidor anterior a CreatedAt se rechaza como conflicto 409 INVALID_INPUT, sin INSERT.

## Contrato implementado T06

`PATCH /api/maintenance-requests/{id}/status` recibe `status` obligatorio como texto y `technicianId` nullable. ID de solicitud positivo; ID de técnico positivo cuando aparece. Assigned exige técnico; los otros destinos prohíben ese campo. Se rechazan miembros JSON internos/desconocidos. Respuesta 200 reutiliza MaintenanceRequestResponse, sin RowVersion ni ETags.

Únicas transiciones: Open → Assigned/Cancelled, Assigned → InProgress/Cancelled, InProgress → Closed. Solicitar el estado actual, reabrir o reasignar no está permitido. Assigned requiere técnico existente activo, sin condición de especialidad. Las transiciones posteriores conservan al técnico y no revalidan su actividad. Closed requiere al menos una visita; Cancelled no requiere visitas. Ambos terminales fijan ClosedAt UTC a precisión de milisegundos, nunca anterior a CreatedAt.

Errores: 400 INVALID_INPUT o validación/formato; 404 REQUEST_NOT_FOUND/TECHNICIAN_NOT_FOUND; 409 INVALID_STATUS_TRANSITION/TECHNICIAN_INACTIVE/VISIT_REQUIRED/CONCURRENCY_CONFLICT. Deadlocks/timeouts conservan el código separado CONCURRENT_CHANGE de T05. Los errores inesperados usan 500 UNEXPECTED_ERROR. T07 no está implementada.

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
