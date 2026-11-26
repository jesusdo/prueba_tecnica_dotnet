# Auditoría técnica T10.1

**Estado vigente T11:** entrega CONDITIONAL; ver sección16. Las referencias al README original en las secciones históricas corresponden ahora a docs/ORIGINAL_CHALLENGE.md, archivado íntegro. No se reescriben sus resultados históricos.

Fecha: 2026-11-26. Base del repositorio: `cfa7623`, rama `dev`, Git limpio al comenzar. Auditoría local con apoyo de Codex; no se realizaron correcciones.

## 1. Resumen ejecutivo

La aplicación funciona en el entorno local y la arquitectura en capas es proporcional al ejercicio. Se reprodujeron **87 pruebas unitarias y 144 de integración aprobadas: 231 total, cero fallidas y omitidas**, con conexión normal. Restore, build, herramientas, auditoría NuGet y publicación Release terminaron con código 0; build sin advertencias ni errores. Los ocho endpoints existen en OpenAPI y se ejercitaron por HTTP real.

Esto **no equivale a cumplimiento funcional completo**. Los reportes conservan deuda aceptada: ValidateReports volvió a producir seis PASS, nueve incumplimientos y error 51910. Se reprodujeron además un rechazo indebido de UPDATE por lote del trigger, incompatibilidad del validador de esquema con el journal del seed y pérdida de rollback si se configura Enlist=false. El README todavía no explica cómo iniciar esta implementación completa.

Se identifican **14 hallazgos consolidados: P0=0, P1=5, P2=7, P3=2**. No se encontraron indicios de secretos personales/corporativos ni vulnerabilidades NuGet reportadas, dentro del alcance descrito. No se certifica seguridad de producción ni ausencia general de condiciones de carrera.

Preparación para entrega: **condicionada** a resolver o comunicar expresamente los P1. El responsable mantiene la autoría y decisión exclusiva sobre correcciones de sus SP manuales. T10.2 y T11 no se iniciaron.

## 2. Alcance, método y límites

- Leídos AGENTS, README original, DECISIONES, requisitos, arquitectura, base de datos, backlog, Compose, configuración, tres proyectos, 60 archivos C# de src, todos los archivos de pruebas y scripts de migrations/scripts/seed/validation. Inspección repartida en lecturas locales acotadas; sin servicios externos de revisión ni envío del código a terceros.
- Evidencia diferenciada: ejecución observada, inspección estática y aspectos NOT VERIFIED. Las pruebas no se modificaron para aprobar y los SP no se alteraron ni se generaron versiones corregidas.
- Sólo se aprovisionó y escribió en bases locales aisladas. Principal: `PropertyMaintenanceDb_T04_T101_20261126`. Las pruebas de migraciones crearon sus propias bases GUID T04_T09_Clean/Existing y las retuvieron conforme a su implementación; no se eliminó ninguna base existente.
- La API temporal usó 8081. La instancia del usuario en 8080 y su base principal no recibieron cambios. Los fixtures E2E, de concurrencia y de sondas se limpiaron por sus claves identificadas; los scripts con transacciones terminaron en rollback cuando correspondía.
- La revisión adicional Gentle AI de T09 fue rechazada por el usuario. El intento de registrar ese rechazo falló por inventario obsoleto después del commit; no inició revisión ni produjo aprobación. No se reactivó ni se ejecutó una revisión externa para T10.1.
- Sólo se escriben este informe y el estado del backlog. No se actualiza DECISIONES en esta fase por la restricción expresa de superficies permitidas; el uso efectivo de IA y sus límites quedan documentados aquí.

## 3. Versiones y entorno observado

| Componente | Valor |
|---|---|
| Host de trabajo | Windows, PowerShell; ejecución .NET dentro de Docker dev |
| Contenedor SDK | Ubuntu 24.04, linux-x64; SDK 10.0.404, MSBuild 18.9.15 |
| Runtime / ASP.NET Core | 10.0.15 |
| SQL Server local | 16.0.4325.4, Developer Edition 64-bit, SQL Server 2022 |
| Framework objetivo | net10.0 en los tres proyectos |
| EF SqlServer / Design / dotnet-ef local | 10.0.7, alineados; Design PrivateAssets=all |
| DbUp | 5.0.40 |
| OpenAPI / Mvc.Testing | 10.0.15 |
| Swagger UI / PKCS | 7.2.0 / 9.0.21 |
| xUnit / runner / Test SDK / coverlet | 2.9.3 / 3.1.4 / 17.14.1 / 6.0.4 |

No se instalaron dependencias nuevas ni herramientas globales. La versión OpenAPI servida fue 3.1.1. Azure SQL no fue contactado: compatibilidad por inspección no sustituye una ejecución real allí.

## 4. Matriz de trazabilidad del enunciado original

Unidad de conteo: 37 requisitos obligatorios verificables, agrupando los campos de cada entidad y separando cada endpoint, regla y mínimo del seed. No se cuentan como requisitos de producto las ponderaciones de evaluación, estimación de horas o sugerencias del entorno. Resultado: **32 PASS, 0 PARTIAL, 3 FAIL, 2 NOT VERIFIED**. PASS significa probado en el alcance local indicado, no una garantía universal.

Rutas de pruebas abreviadas: `IT/` = tests/PropertyMaintenance.IntegrationTests; `UT/` = tests/PropertyMaintenance.UnitTests. Migrations y validaciones están bajo database/. Los textos entre comillas proceden de README; la descripción de campos conserva su contenido sin exigir etiquetas HTTP en español.

| ID | Requisito original / README | Implementación | Prueba o evidencia actual | Estado / hallazgo |
|---|---|---|---|---|
| M01 | C#, .NET 10 y SQL Server 2022/2025, línea 5 | Tres csproj, Compose | build, dotnet --info, SERVERPROPERTY | PASS |
| M02 | Compatible con Azure SQL, línea 5 | T-SQL interno y proveedor SQL Server | Inspección sin incompatibilidad evidente; no despliegue Azure | NOT VERIFIED |
| M03 | Edificio: nombre, dirección, ciudad, línea 25 | 002, Property.cs | ValidateSchema antes del seed | PASS |
| M04 | Unidad: edificio, número, piso, tipo, línea 26 | 001/002, Unit.cs, UnitTypes | ValidateSchema/Seed y GET unidades | PASS |
| M05 | Inquilino: nombre, email único, teléfono, línea 27 | 002, Tenant.cs | ValidateConstraints: email duplicado rechazado | PASS |
| M06 | Contrato: unidad, inquilino, inicio, fin nullable, renta, línea 28 | 002, Lease.cs | ValidateSchema/Constraints/LeaseOverlap | PASS |
| M07 | Técnico: nombre, especialidad, activo, línea 29 | 001/002, Technician.cs | ValidateSchema/Seed; asignación y visita | PASS |
| M08 | Solicitud: campos y técnico/cierre nullable, línea 30 | 002, MaintenanceRequest.cs | DataAccessTests y E2E SQL | PASS |
| M09 | Visita: solicitud, técnico, fecha, notas, costo, línea 31 | 002, Visit.cs | VisitHttpTests y E2E SQL | PASS |
| M10 | Máximo un contrato vigente, línea 35 | Trigger 004 | 15 casos SQL y tres pares de sesiones; protege solapamientos en los casos ejecutados | PASS; F07 limita UPDATE válido adicional |
| M11 | Sólo inquilino con contrato vigente crea solicitud, línea 36 | MaintenanceRequestService.CreateAsync, LeaseRepository | MaintenanceRequestHttpTests, CreationConcurrencyTests, E2E | PASS con configuración normal; F10 condicional |
| M12 | Sólo transiciones Open→Assigned→InProgress→Closed y cancelaciones aprobadas, línea 37 | MaintenanceRequestModel.CanTransition, servicio | UT StatusTransitionTests: 25 pares; IT StatusHttpTests; E2E reapertura 409 | PASS |
| M13 | Asignación requiere técnico activo, línea 38 | Servicio, TechnicianRepository | StatusHttpTests / StatusTransitionTests | PASS |
| M14 | Cierre requiere visita y registra fecha, línea 39 | Servicio, HasVisitsAsync | StatusHttpTests y E2E SQL ClosedAt | PASS |
| M15 | Costos no negativos, línea 40 | DTO, VisitModel, CHECK SQL | VisitHttpTests, UT, ValidateConstraints | PASS |
| M16 | Esquema con PK/FK/restricciones, línea 76 | 001–003 | ValidateSchema antes del seed; cero cascadas observadas | PASS |
| M17 | Esquema versionado recreable, línea 77 | DbUp y recursos 001–007 | Base nueva: siete scripts reales; IT ReportMigrationTests | PASS |
| M18 | Seed reproducible, mínimo 3 edificios, líneas 82–84 | 001_SeedDemoData.sql, DemoSeedVersions | ValidateSeed 01/02 y hashes idénticos tras reinicio | PASS |
| M19 | 30 unidades repartidas, línea 85 | Seed | ValidateSeed 02/03: diez por edificio | PASS |
| M20 | 20 inquilinos con contratos vigentes y vencidos, línea 86 | Seed | ValidateSeed 04–06 | PASS |
| M21 | 5 técnicos, uno inactivo mínimo, línea 87 | Seed | ValidateSeed 07/15 | PASS |
| M22 | 100 solicitudes últimos seis meses, todos los estados y visitas coherentes, línea 88 | Seed | ValidateSeed 08–17; referencia inicial UTC persistida | PASS en base nueva |
| M23 | POST /api/maintenance-requests, línea 100 | MaintenanceRequestsController.Create | HTTP negativos/positivos y E2E 201 con SQL persistido | PASS |
| M24 | POST /api/maintenance-requests/{id}/visits, línea 101 | CreateVisit / VisitService | VisitHttpTests/concurrencia; E2E 201/201/409 | PASS |
| M25 | PATCH /api/maintenance-requests/{id}/status, línea 102 | ChangeStatus | StatusHttpTests y E2E 200/200/200/409 | PASS |
| M26 | GET /api/properties/{id}/units con inquilino vigente o vacío, línea 108 | PropertiesController, PropertyRepository | ReadHttpTests: vigencia/vacancia/límites; HTTP 200 con diez unidades | PASS |
| M27 | GET listado paginado, filtros y datos relacionados, línea 109 | ListAsync, RequestFilter | ReadHttpTests y E2E filtros combinados; SQL OFFSET/FETCH | PASS |
| M28 | GET historial con contratos, solicitudes y costos, línea 110 | TenantsController, TenantRepository | ReadHttpTests con SQL independiente; E2E costo 30.35 | PASS |
| M29 | GET resumen por edificio con métricas en rango, línea 111 | SP PropertiesSummary, ReportRepository | ValidateReports P03/P04/P05 fallan; HTTP 200 no demuestra exactitud | FAIL — F01/F02 |
| M30 | GET carga por técnico activo, visitas/mes/costo y orden por carga, línea 112 | SP TechniciansWorkload | ValidateReports T04–T08, resultados detallados abajo | FAIL — F03/F04; F05 adicional |
| M31 | Al menos un reporte escrito en T-SQL a mano, línea 114 | Ambos archivos canónicos database/scripts | Autoría declarada por desarrollador, commits b1a7270/fc78fb1 y bytes preservados; no atribución a IA | PASS documental |
| M32 | Códigos HTTP adecuados y errores consistentes, línea 116 | ApiController, RequestException, ApiExceptionHandler | Suite HTTP 400/404/409/500 y E2E sin stack traces | PASS |
| M33 | README con ejecución, prueba de endpoints y supuestos, líneas 120/137 | README conserva sólo enunciado/entorno | Compose ejecuta sleep infinity; faltan pasos de esta aplicación en README | FAIL — F06 |
| M34 | DECISIONES: elecciones y qué haría distinto, líneas 138–140 | DECISIONES y riesgos documentados | Lectura de decisiones ORM/capas/SQL/limitaciones | PASS documental |
| M35 | Revelar uso real de IA y validación/correcciones, línea 141 | DECISIONES, este informe | Registros distinguen autoría manual, apoyo IA y resultados | PASS documental |
| M36 | Historial incremental, línea 142 | Git local | Trece commits alcanzables; trabajo SQL manual separado | PASS |
| M37 | Entrega mediante link a repositorio público, línea 9 | Entrega externa | No se verificó publicación, visibilidad ni envío remoto | NOT VERIFIED |

### Extras opcionales, separados de obligaciones

| ID | Extra original / README | Evidencia | Estado |
|---|---|---|---|
| O01 | Pruebas automatizadas, línea 128 | 231 aprobadas y assertions revisadas | PASS |
| O02 | Transacciones/explicación, línea 129 | Scopes y rollback normal; F10 con Enlist=false | PARTIAL |
| O03 | Índices y justificación, línea 130 | 003 y docs/ARCHITECTURE; sin medición de planes | PASS documental/estructural |
| O04 | Logging estructurado/handler global, línea 131 | ILogger y ProblemDetails; F13 no invalida existencia | PASS |
| O05 | Validación de entrada, línea 132 | DTO/servicios y pruebas de límites/formato | PASS |
| O06 | Pipeline CI, línea 133 | No hay workflow versionado identificado | FAIL opcional, no bloquea por sí solo |

La arquitectura V2, 30 días UTC/AsOfUtc, reglas RN adicionales, nombres/tipos exactos, seed detallado, RowVersion, pruebas de lotes y bloqueo concurrente son decisiones aprobadas adicionales. Se auditan abajo, sin atribuir todos esos detalles al README. El arranque con un único Compose es un ideal del enunciado, no obligación independiente; instrucciones completas sí son obligatorias.

## 5. Compilación, publicación y auditoría de dependencias

Comandos ejecutados dentro de dev; la variable de conexión apunta únicamente a la base aislada y sus credenciales no se reproducen aquí:

```text
dotnet tool restore
dotnet restore PropertyMaintenance.slnx
dotnet build PropertyMaintenance.slnx
dotnet test PropertyMaintenance.slnx --collect:"XPlat Code Coverage" --results-directory /tmp/propertymaintenance-t101-results --logger "trx;LogFilePrefix=audit"
dotnet list PropertyMaintenance.slnx package --vulnerable --include-transitive
dotnet publish src/PropertyMaintenance.Api/PropertyMaintenance.Api.csproj --configuration Release --output /tmp/propertymaintenance-t101-publish
```

| Verificación | Código | Resultado observado |
|---|---:|---|
| Tool restore | 0 | dotnet-ef 10.0.7 local restaurado |
| Restore | 0 | Proyectos actualizados |
| Build | 0 | Tres proyectos, 0 warnings, 0 errors |
| Test con conexión normal | 0 | UT 87/87, IT 144/144, skip 0 |
| NuGet directo/transitivo | 0 | Tres proyectos sin vulnerabilidades reportadas por nuget.org |
| Publish Release | 0 | DLL y artefactos publicados en directorio temporal |
| Arranque Release y segunda inicialización | En ejecución, luego parada controlada | Sin scripts pendientes, seed ya aplicado; no se confunde parada con fallo |

NuGet Audit consulta metadatos públicos de paquetes; no se envió el código para revisión externa. El resultado corresponde a las fuentes y fecha observadas, no asegura inexistencia futura de vulnerabilidades.

## 6. Pruebas y cobertura

- La suite normal reproduce exactamente 231 aprobadas; no se detectaron pruebas omitidas. La colección SQL impide paralelismo dentro del proceso y la cadena exige servidor local y prefijo T04. No evita que dos procesos compartan la misma base: es una limitación de aislamiento.
- Assertions significativas: 25 pares de estados, error SQL específico 51021, error de bloqueo 1222, conservación de RowVersion, 409 por conflicto optimista, rechazo sin cambios persistidos, importes/fechas, DTOs controlados por servidor y comparaciones SQL independientes.
- ReportHttpTests caracteriza algunos resultados incorrectos actuales. Es evidencia de fidelidad de integración, no conformidad del reporte. Se mantiene separado del fallo SQL 51910.
- Las pruebas de migraciones retienen dos bases por invocación; esta ejecución agregó otras dos. La repetición consume recursos aunque no borre datos: F11.
- Huecos concretos: UPDATE de varios contratos que desplaza filas del mismo lote; Enlist=false; recuperación ante error parcial del setup; asignación concurrente con desactivación del técnico no instrumentada específicamente. Fallos/limitaciones del oracle de reportes: F09. No se persigue cobertura objetivo arbitraria ni se propone duplicar pruebas de repositorios/servicios sin necesidad.

Cobertura con coverlet ya instalado, sólo paquete PropertyMaintenance.Api:

| Sesión | Líneas | Cobertura de líneas | Cobertura de ramas |
|---|---:|---:|---:|
| Integración | 952/1241 | 76.71% | 58.13% |
| Unitarias | 166/1241 | 13.37% | 28.25% |
| Unión por archivo+número de línea | 957/1241 | **77.12%** | No calculada como unión |

No se sumaron porcentajes. Los XML duplicados por el logger TRX se deduplicaron por identidad archivo/línea y presencia de hits. La instrumentación incluye código generado, por lo que el porcentaje no es una nota de calidad ni una medida exclusiva de reglas manuales. Artefactos temporales: /tmp/propertymaintenance-t101-results (TRX y Cobertura).

Pruebas de diagnóstico aparte de la suite: la sonda de configuración Enlist=false ejecutó una prueba existente sin modificarla y **falló una**, código 1, esperado 100 registros/real 101. Tras limpiar su registro y volver a conexión normal, esa misma prueba pasó. Ese fallo condicional no se oculta ni se cuenta entre las 231 de la ejecución normal.

## 7. Resultados SQL y reportes manuales

Los scripts se enviaron sin cambios a sqlcmd local con `-b`. Cada código se capturó individualmente para continuar tras un fallo conocido.

| Script / condición | Código | Resultado |
|---|---:|---|
| ValidateSchema.sql, antes del seed | 0 | Once tablas, columnas, constraints, índices, RowVersion y SchemaVersions válidos |
| ValidateConstraints.sql | 0 | Casos positivos/negativos, defaults UTC, cero costo y limpieza verificados |
| ValidateLeaseOverlap.sql | 0 | Quince casos aprobados; eliminación de fixtures confirmada |
| ValidateSeed.sql | 0 | Diecisiete controles aprobados usando referencia persistida |
| ValidateReports.sql | 1 | Seis PASS, nueve FAIL conocidos; 51910 tras rollback |
| ValidateSchema.sql, después del seed | 1 | 51001: DemoSeedVersions no está permitido en whitelist; F08 |
| Sonda UPDATE válido por lote, rollback | 0 como diagnóstico | Operación rechazada con 51021; defecto F07 reproducido |

Constraints de importes/fechas, FKs restrictivas, catálogos, RowVersion y trigger habilitado fueron verificados. Consulta de sys.foreign_keys: cero cascadas; sys.triggers: cero deshabilitados. No se encontró NOLOCK ni READ UNCOMMITTED en la lógica auditada. DATE/DATETIME2(3)/DECIMAL(18,2) y catálogos corresponden al diseño. El promedio/costos de SP tienen tipos reales diferentes y están mapeados explícitamente.

### Nueve incumplimientos, individualizados

Fixture base: rango 2026-01-10 al mismo día. `closed-in`: 12:00→13:30 (90 min); `closed-before`: enero 9 23:00→enero 10 00:30 (90 min); `closed-out`: enero 10→enero 11 (1440 min). Visitas: 10.10+20.25 en closed-in; 100 el enero 12 en closed-out; 50 en enero 10 sobre old-pending creado diciembre 1; 7 por técnico distinto en InProgress. Técnico activo A tiene old-pending Assigned e InProgress; técnico activo B realiza la visita de 7 pero no tiene carga. Se agregan luego otras dos visitas de 1 y 2 a old-pending para medir fan-out.

Además del script intacto se ejecutó una copia exclusivamente en memoria con SELECT de observación añadidos antes del rollback. No se cambió ningún archivo, expectativa o error final. Se observaron: edificio `(Open=4, Closed=3, Avg=9, Cost=37.35)`; técnico A `(Assigned=1, InProgress=1, Workload=2, Recent=0, Cost=50)`; después de las dos visitas extra `(Assigned=3, InProgress=1, Workload=2, Recent=0, Cost=53)`.

| Control fallido | Regla y origen | Esperado / real observado | Causa e impacto | Prioridad, recomendación conceptual / intervención |
|---|---|---|---|---|
| P03 | Cerradas con ClosedAt en rango; precisión aprobada de M29 | 2 / **3** | BASE admite CreatedAt dentro y el SUM posterior no filtra ClosedAt; cuenta closed-out | P1, F01: separar población de cada métrica; SP manual, esfuerzo medio |
| P04 | Promedio de resolución en horas fiel, M29; decimales aprobados | 1.5 / **9** | Incluye las tres duraciones; 540 min/60=9. Incluso corrigiendo población, AVG entero/60 daría 1 | P1, F02: población correcta y aritmética decimal; manual, pequeño/medio |
| P05 | Todas las visitas del rango, M29 | 87.35 / **37.35** | Visita 50 de old-pending queda fuera por BASE de solicitudes; gasto subestimado | P1, F01: agregar visitas por su propia fecha; manual, medio |
| P07 | SP rechaza rango invertido; decisión adicional, no falla HTTP | Error definido / **sin error, filas con 0/0/null/0** | SP suma un día a To sin validar; API sí devuelve 400 | P2, F05: validar contrato temporal explícito; manual, pequeño |
| T04 | Activos sin actividad presentes; decisión adicional que precisa M30 | 1 fila B / **0 filas** | WHERE sobre StatusId elimina preservación del RIGHT JOIN | P1, F03: técnicos activos como universo base; manual, medio |
| T05 | Costo acumulado histórico de técnico, M30 | Oracle pide 80.35 / **50.00** | Excluye visitas de Closed; **oracle también es incompleto**: sin límite AsOf, el total del fixture es 180.35 incluyendo visita de enero 12 | P1 F03 y P2 F09: acordar oracle histórico correcto antes de validar corrección; manual SQL y test autorizado |
| T06 | Costos al técnico que visita, decisión aprobada de M30 | B: fila y 7 / **fila ausente, sin valor** | Filtra por técnico asignado y solicitudes pendientes; atribución incorrecta | P1, F03: agregar por Visits.TechnicianId independiente de carga; manual, medio |
| T07 | Últimos 30 días UTC respecto de AsOf fijo; decisión adicional de M30 | 3 a enero 11 / **0** en servidor noviembre 26 | SP ignora parámetro, usa GETDATE; resultado depende del reloj real | P2, F05: usar referencia acordada y límites; manual, pequeño/medio |
| T08 | Múltiples visitas no multiplican carga, M30 | Assigned=1, Total=2 / **Assigned=3, Total=2** | JOIN a visitas antes de SUM; ORDER BY COUNT también depende del fan-out | P1, F04: granularidad de solicitudes antes de combinar visitas y ordenar por carga; manual, medio |

No se convierten estos fallos en PASS por ser deuda aceptada. No se ejecutaron los 29 escenarios originales como 29 controles independientes; los quince controles existentes agrupan varios casos. No se afirma cobertura exhaustiva de fechas futuras o de todos los empates de orden. La condición de no incluir visitas posteriores a AsOf falta por inspección; no se aisló una sonda específica de esa variante.

### Idempotencia y preservación

Después de limpiar E2E/sondas se reinició la API Release con seed habilitado: cero scripts pendientes y demo ya aplicada. Hashes de JSON ordenado de las siete tablas (incluyendo IDs, fechas y RowVersion) y journal antes/después:

```text
Negocio SHA256: 72184311BA7A32FF40DB25604157ED9A09870DE09958EC9C801981B41FA9D009
Journal SHA256: 394B6F6A359D33DE2894C202CA6CC5E05162F43DEA0FC648A4D0D8EFCD831DD7
```

Los dos pares fueron idénticos. Journal: siete entradas de esquema; demo con journal separado. Esto verifica el mecanismo integrado, no que ejecutar el INSERT crudo del seed sin DbUp sea idempotente.

### Flujo End-to-End por HTTP real

Instancia temporal localhost:8081, sin mocks ni TestServer para este recorrido. Los identificadores se obtuvieron de consultas al seed, sin asumir que IDENTITY comienza en 1: edificio 3, unidad 5, inquilino 6, técnicos activos 2 y 3.

| Paso | Resultado observado |
|---|---|
| Crear solicitud | 201, RequestId 256, Open, técnico/cierre null |
| Asignar técnico 2 | 200, Assigned |
| Iniciar | 200, InProgress, técnico 2 conservado |
| Visita por técnico asignado | 201, VisitId 174, costo 10.10 |
| Visita por otro técnico activo | 201, VisitId 175, técnico 3, costo 20.25 |
| Cerrar | 200, Closed, técnico 2 conservado y ClosedAt UTC |
| Visita después de cierre | 409, INVALID_VISIT_STATUS; no se agregó visita |
| Reabrir/asignar tras cierre | 409, INVALID_STATUS_TRANSITION |
| Consultar unidades | 200, edificio 3 con diez unidades |
| Listar filtrando Closed/High/edificio 3, página 1 tamaño 5 | 200, solicitud 256 primera con datos relacionados |
| Historial del inquilino 6 | 200, solicitud 256 con TotalVisitCost 30.35 |
| Ambos reportes | 200; resumen edificio 3: Open 31, Closed 20, promedio 11, costo 3315.35; técnicos 2/3/4: carga 14/13/13 y costo reportado 0 |
| SQL independiente | RequestId 256, StatusId 4, técnico 2, RowVersion de ocho bytes, exactamente dos visitas por 30.35 |

CreatedAt=2026-11-26T11:03:37.088Z; visitas 11:03:37.501Z y 11:03:37.547Z; ClosedAt=11:03:37.568Z. El reporte de técnicos omitió los nuevos costos históricos sobre la solicitud ya cerrada: discrepancia observada F03, no un PASS semántico por recibir 200. Se limpiaron únicamente solicitud 256 y visitas 174/175 después de verificar SQL. El primer intento del cliente de auditoría no cargó System.Net.Http en PowerShell; se cargó la biblioteca y se repitió correctamente, sin cambiar la aplicación.

SP preservados, SHA256 inicial/final:

```text
PropertiesSummary: E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F
TechniciansWorkload: FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5
```

## 8. Arquitectura, C# y seguridad

- Tres proyectos, todos net10.0, ambos tests referencian Api y Api no referencia tests. DbContext Scoped mediante AddDbContext. No ciclos de proyectos observados.
- Controllers delegan; no contienen SQL ni agregaciones. Servicios gestionan reglas y transacciones; repositorios concretos contienen EF/SQL parametrizado. Entidades generadas, modelos de negocio y DTOs están separados; mapeo explícito. Sin repositorio genérico, Unit of Work genérico, MediatR, CQRS, AutoMapper ni infraestructura excesiva. No se recomienda cambiar de arquitectura.
- NRT habilitado; async/CancellationToken y liberación mediante using/await using en accesos normales. Excepciones esperadas y validación diferenciadas. DBNull/NULL de reportes representados según el SQL actual. No se identificó dependencia nueva sin uso que justifique eliminación en esta fase.
- Fechas de escritura usan UTC/TimeProvider y truncado a ms; DATETIME2 leído se marca UTC conforme a convención de almacenamiento, no convierte datos históricos mal almacenados. SQL Server no guarda zona en DATETIME2. Relojes entre instancias no monotónicos pueden producir cierre anterior a una visita; posibilidad estática no reproducida, no regla original adicional impuesta aquí.
- Costos usan decimal, validación de escala/rango antes de persistir y converter de JSON para evitar redondeo silencioso de números extremos. DECIMAL(38,2) fuera de CLR provoca OverflowException, verificado por prueba existente. No se sustituye por cero.
- DTOs de escritura rechazan campos no mapeados: no aceptan controlar IDs generados, fechas, estados ajenos al endpoint o RowVersion. Pruebas existentes cubren identificadores, enums como texto, longitudes, importes, fechas y overflow de paginación.
- SQL interpolado EF genera parámetros; SqlParameter en SP; LINQ parametrizado. El nombre aleatorio de base en pruebas se forma internamente, no desde HTTP. No se observó concatenación de entrada del cliente en SQL.
- Handler no devuelve stack traces, credenciales ni mensajes SQL al cliente; el ensayo de errores inesperados está aprobado. ILogger del handler usa un texto de operación inexacto (F13); no se encontró EnableSensitiveDataLogging. No se garantiza que todo proveedor/excepción futuro nunca incluya un dato sensible: límite de inspección.
- Escaneo local de texto versionado y 13 commits/206 blobs únicos: patrones de credenciales, claves privadas y tokens conocidos. Se identificó únicamente la credencial pública del template de desarrollo en .env.example, Compose, README y documentación/historial. **No se reproducen sus valores**. No se identificaron secretos personales/corporativos adicionales. No abarca objetos Git inalcanzables ni garantiza ausencia de formatos arbitrarios de secreto.
- Compose publica en todas las interfaces del host y utiliza administrador SQL de desarrollo: F12. No se realizó sondeo remoto ni se afirma que el firewall permita acceso externo. Autenticación/autorización fuera de alcance; es limitación para exposición productiva, no incumplimiento del ejercicio.

## 9. Concurrencia comprobada y límites

| Área | Evidencia ejecutada | Resultado y alcance |
|---|---|---|
| Contratos INSERT/UPDATE, inclusividad/NULL/lotes | ValidateLeaseOverlap 15 casos | PASS de los casos existentes; F07 revela hueco en UPDATE conjunto válido |
| Contratos incompatibles, dos sesiones | Scripts SameUnit A/B; B sólo se lanzó tras observar A en WAITFOR por DMV | A commit, B error 51021; estado final 2040: una lease |
| Contratos compatibles, misma unidad | Scripts Compatible A/B, misma coordinación | Ambos commit; 2041: dos leases; espera real SPID 85 bloqueado por 70, LCK_M_X |
| Unidades diferentes | Scripts DifferentUnit A/B | Ambos commit; 2042: dos leases; no bloqueo en muestra tomada |
| Creación frente a cambio de contrato | CreationConcurrencyTests.IndependentConnectionCannotInvalidateLeaseBetweenValidationAndInsert | PASS; segundo SqlConnection con Enlist=false obtiene 1222, no modifica lease, solicitud confirma |
| Estado/RowVersion | StatusHttpTests.IndependentTransactionsProduceRowVersionConflictAndHttp409 | PASS; SPIDs distintos, transacción ganadora confirma, versión original conservada y HTTP 409 CONCURRENCY_CONFLICT |
| Visita/cancelación y visita/cierre | VisitConcurrencyTests.VisitAndTerminalTransitionSerializeAcrossIndependentSessions | Dos casos PASS; DMV verifica bloqueo LCK_M_*, luego commit en orden válido y visita terminal rechazada |
| Visita/desactivación | TechnicianDeactivationWaitsUntilValidatedVisitCommits | PASS; otra transacción espera al técnico y sólo desactiva después del commit |
| Atomicidad con configuración normal | FailureAfterSaveRollsBackEntireCreation y FailureAfterVisitSaveRollsBackInsertion | PASS; post-SaveChanges fallido se revierte |
| Configuración Enlist=false | Misma prueba de rollback de creación, sin cambiar código | FAIL reproducido 100→101, fila limpiada; configuración normal reprobada después: PASS |

En la primera pareja también se observó bloqueo 85→70 LCK_M_X. Cada script tuvo código 0; B incompatibles maneja únicamente el error 51021 como esperado. No se confunde ese código 0 de validación con que ambas transacciones confirmaran.

Trigger: applocks transaccionales por UnitId afectado, orden ascendente, timeout 10s; se comprueban intervalos inclusivos y lote. Servicios: creación Serializable; estados/visitas ReadCommitted con parent UPDLOCK,HOLDLOCK antes de técnico/hijos y técnico HOLDLOCK. Conexión normal comparte DbContext Scoped y enlistamiento ambiente. La lectura original de estado antes del bloqueo conserva la RowVersion para detectar una transición ganadora, no la reemplaza por versión reciente.

No se observaron deadlocks en estos schedules; eso no demuestra ausencia general. No se capturó deadlock graph ni se ejecutó estrés. No se instrumentó TransactionInformation.DistributedIdentifier: promoción a DTC **NOT VERIFIED**. Una segunda conexión enlistada dentro del scope o cambios de repositorios podrían promover; los caminos actuales comparten contexto, pero no se afirma inmunidad general. Los tests de RowVersion que comparten una transacción por sí solos no prueban carrera real; aquí se contó por separado la prueba HTTP con transacciones/SPIDs independientes.

## 10. Rendimiento: hechos frente a hipótesis

La prueba existente SqlProjectionsUseBoundedQueriesAndServerSideOperations volvió a pasar y se inspeccionó su SQL real:

```sql
WHERE [m].[StatusId] = @status AND [u].[PropertyId] = @property
  AND [m].[CreatedAt] >= @from AND [m].[CreatedAt] < @to
ORDER BY [m].[CreatedAt] DESC, [m].[RequestId] DESC
OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY
-- Historial: agregado correlacionado, sin JOIN a contratos:
COALESCE((SELECT COALESCE(SUM([v].[Cost]),0.0)
          FROM [Visits] [v] WHERE [m].[RequestId]=[v].[RequestId]),0.0)
```

Observado: dos comandos para edificio/unidades, dos para conteo/página, tres para historial. LEFT JOIN preserva vacantes y técnico nullable; proyecciones evitan Email/RowVersion innecesarios; filtros, orden, agregación y paginación se ejecutan en SQL. AnyAsync para existencias; sin consultas por fila, lazy loading o Include indiscriminado en estas lecturas. No operaciones paralelas sobre el mismo DbContext.

Índices relevantes ya existentes: Units(PropertyId,Number) único, Leases(UnitId,StartDate,EndDate), Leases(TenantId), MaintenanceRequests(UnitId,CreatedAt), MaintenanceRequests(TenantId), TechnicianId/StatusId y Visits(RequestId). Se revisó su correspondencia con los accesos; **no se inspeccionaron planes reales ni se afirma qué índice fue elegido**. Un índice global CreatedAt/RequestId sólo sería una hipótesis a medir con mayor volumen.

El fan-out de reportes es un problema de corrección demostrado, no sólo de rendimiento. Offset profundo y conteo separado tienen costos/consistencia documentados; cambios concurrentes pueden desplazar páginas. No se midieron throughput, p95 ni cargas productivas, no se extrapolan tiempos de consultas sobre cien solicitudes y no se agregaron índices.

## 11. Hallazgos consolidados y prioridad

Las referencias son rutas del repositorio y líneas del estado auditado. Cada fila incluye categoría, requisito, evidencia/impacto, recomendación conceptual, esfuerzo e intervención. No se ofrecen parches ni SP corregidos.

| ID / prioridad | Categoría y requisito | Archivo/línea y evidencia | Impacto / recomendación | Esfuerzo / autorización |
|---|---|---|---|---|
| F01 P1 | Funcional SQL, M29 | database/scripts/dbo.usp_GetPropertiesSummary.sql:28,43; filtro inicial mezcla CreatedAt/ClosedAt; P03 3 vs 2, P05 37.35 vs 87.35 | Conteos/gasto incorrectos; definir poblaciones independientes por métrica | Medio; desarrollador corrige SP manual y autoriza nueva migración |
| F02 P1 | Precisión SQL, M29 | Mismo SP:45; AVG(INT)/60; P04 9 vs 1.5, con población correcta todavía 1 | Horas truncadas; aritmética decimal sin mezclar población | Pequeño/medio; intervención SQL manual |
| F03 P1 | Universo/atribución SQL, M30 | database/scripts/dbo.usp_GetTechniciansWorkload.sql:20–31,40; T04/T06 sin filas, T05 50 vs histórico | Oculta técnicos y costos reales; separar activos, carga y visitas por técnico visitante | Medio; intervención SQL manual, sin reinterpretar sólo Assigned/InProgress para visitas |
| F04 P1 | Granularidad/orden SQL, M30 | Mismo SP:36–45; T08 Assigned=3, Workload=2; COUNT de join para ordenar | Carga y ranking inconsistentes; agregado por solicitud y orden por métrica de carga | Medio; intervención SQL manual |
| F05 P2 | Contrato temporal adicional | Properties SP:8, Technician SP:39; P07 no rechaza, T07 0 vs 3 | HTTP sí protege rango; SP no respeta AsOf ni frontera superior; acordar y validar referencia/límites | Pequeño/medio; manual, no cambiar reglas unilateralmente |
| F06 P1 | Entrega/documentación, M33 | README.md:58–66,120,137; compose.yaml:25; Program.cs:52–56 | Falta recorrido de ejecución real: aprovisionar DB, conexión, seed, API y Swagger; añadir quick start conservando enunciado | Pequeño; autorización T10.2/T11 para docs, Compose sólo si se aprueba |
| F07 P2 | Integridad de lotes aprobada | database/migrations/004_CreateLeaseOverlapTrigger.sql:69–80; enero/febrero movidos a febrero/marzo en un UPDATE válido reciben 51021 | Rechaza corrección válida, no evidencia de corrupción; comparar lote propuesto contra filas persistidas no afectadas | Medio; nueva migración autorizada, nunca editar 004 aplicada |
| F08 P2 | Validador de esquema | database/validation/ValidateSchema.sql:19–27; 51001 tras crear DemoSeedVersions | Falso fallo en estado soportado; reconocer journals aprobados | Pequeño; autorización para modificar validación, no realizada |
| F09 P2 | Oracle de pruebas de reportes | database/validation/ValidateReports.sql:40,45,63; promedio capturado INT; costo esperado 80.35 omite visita histórica 100 | Un SP correcto podría seguir fallando: total histórico del fixture 180.35, y columna INT impide probar 1.5 | Pequeño/medio; acordar contrato y autorizar prueba después de corrección manual, sin ocultar fallo actual |
| F10 P2 | Configuración/transacciones | Program.cs:30–31,52–57; MaintenanceRequestService.cs:67–69; repositorio SaveChanges; prueba CreationConcurrencyTests:84–96 | Enlist=false aceptado deja fila tras excepción: 100→101; rechazar esa opción o administrar transacción explícita compatible | Pequeño para guard, medio para alternativa; autorización de código/configuración |
| F11 P2 | Aislamiento/ciclo de vida de pruebas | IT/ReadFixtures.cs:33–56, VisitHttpTests.cs:22–29, ReportHttpTests.cs:86–92, ReportMigrationTests.cs:43–55 | Setup puede dejar filas si falla antes de devolver handle; cada suite retiene dos bases. Parte estática y retención observada | Medio; autorizar cambios de fixtures y un plan explícito de limpieza, nunca DROP implícito |
| F12 P2 | Exposición local adicional | compose.yaml:10–12,31; .env.example:2 | Credencial pública del template y puertos en todas las interfaces; alcance de red depende del host | Pequeño; evaluar bind loopback/credencial local propia con autorización, no afirmar secreto corporativo filtrado |
| F13 P3 | Observabilidad opcional | src/PropertyMaintenance.Api/Exceptions/ApiExceptionHandler.cs:18 | Todo error inesperado se anuncia como creación de solicitud, incluso reportes/visitas; mensaje neutro con path/método | Pequeño; cambio de código requiere autorización |
| F14 P3 | Claridad documental | docs/REQUIREMENTS.md:31,49; docs/DATABASE.md:70; notas históricas backlog | Estados antiguos sin contexto inmediato dificultan reconstruir estado vigente; etiquetar snapshots y mantener sección actual | Pequeño; autorización de docs, no corregido en T10.1 |

F11 usa prefijo IT abreviado definido en sección 4. No se elevan preferencias de formato, carpeta o arquitectura a defectos. Ausencia de CI, pin de SDK/imagen o métricas de producción son mejoras opcionales, no P1 adicionales. No se detectó P0 dentro del alcance; esto no constituye certificación de ausencia de riesgo grave en cualquier configuración.

## 12. Riesgos aceptados temporalmente

El responsable aceptó en T09 integrar sus SP sin cambios por tiempo y autoría manual. F01–F05 siguen vigentes; esa aceptación no cambia la matriz original ni los valores esperados. F09 exige revisar también el oracle antes de usar una ejecución verde como prueba de corrección.

Persisten límites documentados: consistencia entre consultas de lectura, ventanas de offset bajo escrituras, fechas UTC por convención, único inicializador durante despliegue, credenciales/puertos del entorno de desarrollo y retención de bases de prueba. Autenticación/productivización no están incluidas. No se comprobó Azure ni publicación del repositorio. El rechazo de Gentle AI no elimina ni rebaja hallazgos.

## 13. Recomendaciones para una T10.2 futura

1. El desarrollador decide y corrige manualmente los reportes: F01–F05. Registrar nuevas migraciones sólo después de su autorización; preservar el historial y la atribución real.
2. Acordar el costo histórico del fixture y el tipo de promedio del oracle: F09. Mantener evidencia roja hasta que resultado y expectativa sean válidos.
3. Completar ejecución reproducible en README conservando el enunciado: F06. No exige cambiar Compose para lograr un único comando.
4. Autorizar guard de enlistamiento, validación del journal y trigger de UPDATE por lote mediante nueva migración: F10/F08/F07. Cada corrección debe tener un caso de regresión proporcional.
5. Mejorar fixtures y plan de limpieza de bases con permiso expreso; evaluar exposición local según contexto: F11/F12. Dejar F13/F14 para limpieza posterior si el tiempo lo permite.

Nada de esta lista se implementa automáticamente. La auditoría no autoriza SQL, código, configuración, eliminación de bases, commit, push o avance a T11.

## 14. Preparación para entrega y cierre de alcance

La solución es ejecutable con configuración explícita y sus seis endpoints no relacionados con reportes cumplen los casos funcionales ejecutados. Tiene buenas bases de validación y concurrencia, con pruebas SQL reales y separación de capas defendible en entrevista. Los reportes no cumplen todos los indicadores originales y el README no guía todavía a un evaluador desde un checkout limpio. Por eso la recomendación es **entrega condicionada**, no aprobación integral.

Verificación final de limpieza: 3 edificios, 30 unidades, 20 inquilinos, 26 contratos, 5 técnicos, 100 solicitudes y 110 visitas; cero fixtures identificables T09/T101/concurrencia. API temporal detenida. Los hashes de ambos SP siguen coincidiendo. Bases aisladas y artefactos de pruebas no se confunden con cambios versionados; no se eliminó ninguna base existente.

Se conserva como evidencia positiva la suite 231/231, sin usarla para neutralizar SQL 51910, el fallo del validador después del seed ni el ensayo negativo de enlistamiento. No se modificó código funcional, SP, migraciones, seed, validaciones, pruebas, paquetes ni configuración. Sólo este informe y docs/BACKLOG.md. No commit ni push; T10.1 ejecutada, T10 permanece incompleta, T10.2/T11 no iniciadas.

## 15. Remediación T10.2 — alcance autorizado

Esta sección añade evidencia posterior sin reescribir la auditoría histórica. Base: 67c8bbd, rama dev, Git limpio al comenzar. Autorización exclusivamente para F07, F08 y F10, sus regresiones y documentación. No se modificaron SP manuales, sus recursos, migraciones 001–005, ValidateReports, README, Compose, esquema EF ni estrategia de transacciones. T11 no iniciada.

### Correcciones y evidencia RED → GREEN

| Hallazgo | Causa confirmada | Corrección mínima | Regresión y evidencia posterior | Estado |
|---|---|---|---|---|
| F07 | El propuesto de una fila se compara con el estado anterior de otra fila incluida en el mismo UPDATE; el estado final válido no importa al validador antiguo | Nueva 008 altera el trigger y excluye todos los LeaseId de deleted de la comparación con persistidos. Mantiene comparación entre filas propuestas, fechas inclusivas/NULL, 51021 y bloqueos por unión de unidades nuevas/anteriores en orden ascendente | RED: 3 de 17 casos fallaban (desplazamiento, intercambio de períodos y de unidades). GREEN: 19 casos de leases, incluyendo EF y dos concurrencias nuevas. No se editó 004 | RESOLVED |
| F08 | La whitelist sólo permitía SchemaVersions; el seed crea DemoSeedVersions | Dos nombres de journals exactos separados de las once tablas requeridas. Todo el bloque de columnas, tipos y constraints permanece sin cambios | RED: base inicializada falla51001; tres tests: 1 fallaba. GREEN: 3/3; Missing Properties sigue51000, tabla inesperada51001. ReportMigrationTests valida también sin seed | RESOLVED |
| F10 | TransactionScope requiere inscripción automática; Enlist=false permite SaveChanges fuera del scope | SqlConnectionStringBuilder valida el valor efectivo al arrancar; default/true válidos, false rechazado antes de DbUp/Run. Errores fijos sin cadena ni excepción interna | RED de arranque: llegaba a resolución de DbUp. GREEN: 12 unitarias +1 startup; artefacto publicado con false sale1 sin migraciones/HTTP. Rollback de creación/visitas y conflicto de estado: 3/3 con Enlist=true explícito | RESOLVED |

Program valida después de Build para respetar configuración efectiva de WebApplicationFactory, antes de resolver migrador. No se movieron responsabilidades de transacciones: CreateAsync usa Serializable, ChangeStatusAsync/CreateVisit usan ReadCommitted y bloqueos ya existentes. El guard protege el arranque de esta API, no construcciones manuales con otra cadena fuera de ese arranque. No se instrumentó promoción DTC ni se garantiza ausencia general de deadlocks.

### Regresiones y ejecución final

Suite Release final: **99 unitarias +167 de integración =266 aprobadas, 0 fallidas y 0 omitidas**. Las 231 anteriores siguen aprobadas; se añaden **35 casos**: 12 configuración, 1 arranque, 3 esquema y 19 leases. Los cuatro casos de ReportMigrationTests ya existentes se conservan: sus expectativas pasan legítimamente de siete a ocho recursos por la migración nueva, y la ruta de actualización aplica realmente 001–007 antes de 008. No se modificaron aserciones de indicadores de reportes para hacerlas aprobar.

Comandos ejecutados en dev: dotnet tool restore; dotnet restore PropertyMaintenance.slnx; dotnet build PropertyMaintenance.slnx -c Release; dotnet test PropertyMaintenance.slnx -c Release; dotnet list PropertyMaintenance.slnx package --vulnerable --include-transitive; dotnet publish src/PropertyMaintenance.Api/PropertyMaintenance.Api.csproj -c Release --output /tmp/propertymaintenance-t102-publish. Todos código0; build con cero warnings/errores; NuGet sin vulnerabilidades reportadas en los tres proyectos. Sin dependencias nuevas. No se volvió a medir cobertura ni se reclama un porcentaje nuevo.

Base principal de regresión: PropertyMaintenanceDb_T04_T102_20261126, creada sin eliminar ni modificar bases existentes. Las pruebas DbUp usan sus bases aisladas propias; no se ejecutó contra la base principal de desarrollo.

| Verificación SQL posterior | Resultado |
|---|---|
| ValidateSchema.sql con seed | Código0 |
| ValidateSchema sin seed | Aprobado en ambas rutas clean/upgrade de ReportMigrationTests |
| ValidateConstraints.sql | Código0, casos positivos/negativos y limpieza |
| ValidateLeaseOverlap.sql | Código0, 15 casos originales |
| ValidateSeed.sql | Código0, 17 controles |
| ValidateReports.sql, sin cambios | Código1, **6 PASS /9 incumplimientos, error51910** |
| Journals | Ocho entradas 001–008, ordenadas; segunda ejecución conserva IDs/nombres/fechas |
| SP en upgrade | Definiciones byte a byte idénticas antes/después de 008; recursos canónicos también iguales |

Concurrencia: se repitieron las tres parejas SQL existentes, lanzando B sólo después de observar A en WAITFOR mediante DMV. Misma unidad incompatible: A confirma, B51021, una lease2040; compatibles: ambas confirman, dos leases2041; unidades distintas: ambas confirman, dos leases2042. Se observaron SPID70 bloqueado por62 con LCK_M_X en los dos primeros casos; sin bloqueo en la muestra de unidades distintas. Códigos de validación0 en las seis sesiones; los scripts manejan únicamente51021 como rechazo esperado. Se limpiaron esos fixtures identificables.

Además, dos regresiones nuevas con conexiones/transacciones distintas realizan el UPDATE válido por lote y sostienen su transacción: la inserción incompatible espera y falla51021; la compatible espera y confirma. Las pruebas verifican SPIDs distintos, bloqueo DMV antes del commit, estado final sin superposiciones y limpieza. Ninguno de estos resultados implica inmunidad universal a deadlocks.

Verificación local independiente sobre snapshot temporal de los cambios: no encontró un blocker reproducido; confirmó que desde ExpectedColumns en adelante el validador permanece idéntico y que 008 sólo cambia ALTER y la exclusión de versiones anteriores. No fue revisión de terceros/Gentle AI. Sus comprobaciones fueron estáticas; la ejecución SQL/test anterior es evidencia del proceso principal. Snapshot temporal limpiado después de verificar su ruta; sin cambios fuera del alcance.

### Dos requisitos NOT VERIFIED

| ID | Evidencia adicional obtenida | Límite y siguiente validación | Estado |
|---|---|---|---|
| M02 Azure SQL | Revisión de scripts internos: sin CREATE DATABASE/USE ni dependencias de servidor detectadas. Microsoft documenta Azure SQL para sp_getapplock y triggers INSTEAD OF; 008 conserva esos mecanismos. Pruebas reales sólo en SQL2022 local | No hay destino/identidad Azure autorizados. Ejecutar migraciones y regresiones en una base Azure previamente aprovisionada requiere intervención humana/T11; inspección no equivale a despliegue | NOT VERIFIED |
| M37 link a repositorio público | Configuración Git local tiene origin HTTPS en GitHub, sin credenciales embebidas | No se contactó remoto, verificó visibilidad ni realizó publicación/envío. El desarrollador debe comprobar enlace público y entrega en T11 | NOT VERIFIED |

Fuentes técnicas oficiales consultadas, sin enviar código: [sp_getapplock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver16) y [CREATE TRIGGER](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql?view=sql-server-ver16). La matriz obligatoria histórica permanece 32 PASS, 3 FAIL, 2 NOT VERIFIED: estas tres correcciones adicionales no resuelven los dos reportes ni README.

### Estado individual y propuestas fuera de alcance

Sólo las filas RESOLVED anteriores se implementaron. Las propuestas siguientes quedan para decisión expresa; no constituyen autorización para ejecutarlas ni generan una versión corregida de los SP.

| ID / prioridad | Causa e impacto real | Obligatorio / T11 | Corrección mínima propuesta | Riesgo de regresión / intervención | Estado |
|---|---|---|---|---|---|
| F01 P1 | Poblaciones de fechas mezcladas alteran cierres/gastos | M29; no es principalmente entrega | Separar las poblaciones de cada indicador y probar bordes | Medio: afecta totales; **corrección SQL personal del desarrollador**, nueva migración autorizada | OPEN |
| F02 P1 | Aritmética entera pierde horas fraccionarias | M29; no T11 principalmente | Acordar decimal y población correcta del promedio | Medio: tipo/resultset cambia; SQL manual y ajuste de mapeo sólo autorizado | OPEN |
| F03 P1 | Base de pendientes/igualdad de asignado omite visitantes, finalizadas y activos sin carga | M30; no T11 principalmente | Independizar técnicos activos, carga y actividad del técnico visitante | Medio: universo y costos; SQL manual, autorización adicional | OPEN |
| F04 P1 | JOIN de visitas multiplica conteos/orden | M30; no T11 principalmente | Agregar a granularidad de solicitud antes de combinar actividad | Medio: ranking; SQL manual, autorización adicional | OPEN |
| F05 P2 | SP no valida rango ni respeta AsOf acordado | Detalle adicional; API sí valida; no T11 principalmente | Acordar validación/límites UTC y parámetro | Medio: temporalidad de resultados; SQL manual, autorización adicional | OPEN |
| F06 P1 | README no contiene la ejecución/pruebas de esta aplicación | M33; **principalmente T11** | Añadir quick start sin borrar enunciado: aprovisionar, conexión, seed, API, Swagger | Bajo: comandos deben comprobarse; autorización T11, no cambio Compose implícito | DEFERRED |
| F07 P2 | Versiones anteriores del mismo lote se comparaban como ajenas | Decisión adicional de integridad | Aplicada 008 y regresiones | SQL compartido: riesgo acotado por pruebas; futuras variantes siguen requiriendo comprobación | RESOLVED |
| F08 P2 | Journal de seed legítimo rechazado | Verificación adicional | Whitelist exacta y tests negativos, aplicada | Bajo; metadata restante intacta | RESOLVED |
| F09 P2 | Oracle captura promedio INT y omite costo100 en expectativa histórica | Aseguramiento de M29/M30; no T11 principalmente | Acordar contrato histórico, ajustar oracle después de decisión SQL; mantener evidencia roja | Medio: riesgo de aprobar incorrectamente; autorización para pruebas, no modificado | OPEN |
| F10 P2 | Enlist=false desactivaba atomicidad del scope | Robustez adicional, soporta reglas | Guard temprano aplicado, con configuración true/default | Bajo para configuración soportada; consumidores manuales fuera del arranque no cubiertos | RESOLVED |
| F11 P2 | Setup parcial puede dejar fixtures; pruebas retienen bases nuevas | Calidad de tests opcional; limpieza puede planearse T11 | Hacer ownership antes del primer write y acordar retención/limpieza | Medio: cleanup podría borrar datos; autorización adicional y selección exacta antes de cualquier DROP | OPEN |
| F12 P2 | Puertos en todas las interfaces y credencial pública del template | No fallo de autenticación del ejercicio; **T11/entorno** | Evaluar loopback y credencial local propia según despliegue | Medio: puertos/volumen existentes; autorización de Docker/credenciales | DEFERRED |
| F13 P3 | Mensaje global dice creación para errores de otras operaciones | Logging opcional; no bloquea T11 | Mensaje neutro y contexto estructurado de ruta/método | Bajo: sólo diagnósticos; cambio de código requiere autorización | OPEN |
| F14 P3 | Notas históricas dificultan identificar estado vigente | Documentación, indirectamente M33; **T11** | Etiquetar snapshots y consolidar estado actual sin borrar historia | Bajo; autorización adicional para limpieza global, no implementada | DEFERRED |

Totales actuales: **3 RESOLVED, 8 OPEN y 3 DEFERRED**. Permanecen los cinco P1 originales; T10 no se cierra completamente y la preparación de entrega sigue condicionada por exactitud de reportes/instrucciones. No se cambia la prioridad de la deuda para mejorar artificialmente el resultado.

### Preservación e intervención humana

SHA256 antes y después de T10.2, sin diferencias:

```text
PropertiesSummary: E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F
TechniciansWorkload: FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5
```

No se editaron recursos embebidos ni archivos manuales. F01–F05 requieren corrección/decisión personal del desarrollador; F09 conserva su discrepancia de oracle explícita. M02/M37 requieren evidencia externa autorizada o validación humana. README/Docker/entrega definitiva se difieren, sin avance automático. No se hicieron commit ni push.

## 16. T11: preparación final y validación de entrega

Base92d5435, rama dev, Git limpio al comenzar. Cambios exclusivamente de documentación, configuración local, scripts de ejecución y CI: no se modificaron endpoints, reglas, código de src/tests, dependencias, ocho migraciones, SP, recursos embebidos ni ValidateReports. El enunciado del README anterior se copió íntegro a ORIGINAL_CHALLENGE.md. El README nuevo describe instalación, arquitectura Mermaid, ocho contratos, flujo, pruebas, SQL, publish y limitaciones.

### Evidencia desde copia local limpia

Se creó una **copia local candidata**, no un clon remoto, con148 archivos registrados y no registrados que constituyen el cambio. Se excluyeron bin/obj, .git, configuraciones personales y .env local; se generó una configuración efímera propia. Volúmenes SQL y NuGet nuevos bajo proyecto explícito propertymaintenance-t11-clean-20261126, puertos15433/18080/18081. Las modificaciones finales sólo documentales y de nombre de CI se sincronizaron después sin reutilizar artefactos del desarrollador.

La imagen Dockerfile se construyó desde fuente y publicó Release; se ejecutó el contenedor runtime como app, con ASP.NET10.0.15 y sin SDK. El servicio dev conserva SDK10.0.404 y el flujo existente. Las instrucciones del README se ejecutaron con el -p explícito documentado para revisión paralela:

| Paso | Resultado real |
|---|---|
| Config Compose/healthcheck y dos bases nuevas | Código0; volumen nuevo y SQL2022 sano; aprovisionamiento no aplica esquema |
| Build imagen API / arranque | Código0; DbUp aplicó001–008 y seed, antes de escuchar |
| dotnet tool restore / restore | Código0, herramienta local10.0.7 y dependencias descargadas a cache nueva |
| Build Release | Código0, cero warnings/errores |
| Test Release | Código0: **99 unitarias/167 integración,266 PASS,0 FAIL,0 SKIP** |
| NuGet directo/transitivo | Código0, tres proyectos sin vulnerabilidades reportadas; JSON de auditoría y condición equivalente a CI verificados |
| Publish Release | Código0, artefacto generado en /tmp/propertymaintenance-publish |
| Swagger/OpenAPI | HTTP200 en API publicada18081 y modo dev18080; OpenAPI3.1.1 con8 operaciones |
| SQL wrapper | Esquema, constraints,15 leases y17 seed checks aprobados; después reportes6 PASS/9 fallos, **51910/código1 propagado** |
| Reinicio API | Cero scripts pendientes; demo ya aplicada;100 solicitudes110 visitas,8 journal esquema/1 seed |

Los ocho endpoints se ejercitaron mediante curl contra la imagen publicada. IDs obtenidos del seed por consulta/helper y respuesta, no supuestos: unidad/inquilino/edificio/técnico1 en esa instalación nueva. Request101: Open201, Assigned200, InProgress200, visita111 por12.34 con201, Closed200, historial/listado/unidades200 y reportes200; visita posterior409 INVALID_VISIT_STATUS. SQL confirmó exactamente una visita por12.34 y se limpiaron sólo esos dos registros de prueba. El promedio/costo de reportes se mantiene como salida técnica, no como indicador validado funcionalmente.

Después de limpiar el flujo, hash integral ordenado de las siete tablas antes/después de reiniciar: **9E39463B84ECDACA0B9C4CF3A5FD8D0EE5827FD721A2E6EE655E4A59CC0DBB1E**, idéntico. Esto comprueba identidades, fechas, RowVersion y datos sin duplicación. Pruebas y validaciones usaron la base T04_Integration aislada; no una base previamente inicializada.

### Incidente de aislamiento y recuperación

El primer intento de validación generó por error un .env de una sola línea debido a precedencia de expresiones PowerShell. No se aplicó el nombre de proyecto y Compose reutilizó el predeterminado: recreó temporalmente los contenedores db/dev originales y SQL salió1. Este intento fallido **no** se cuenta como prueba limpia satisfactoria.

Se suspendió la ejecución, se inspeccionaron sólo rutas/etiquetas/volúmenes y se restauró el Compose exacto de HEAD con el mismo volumen. db/dev volvieron sanos y28 bases de usuario estaban ONLINE. No se eliminaron volúmenes ni se ejecutó DML/DDL de negocio sobre la base principal. No había hash previo de esa base y no se afirma igualdad byte a byte ni ausencia de interrupción; el reinicio es un efecto real comunicado al responsable.

La repetición satisfactoria usó ocho líneas verificadas de entorno, -p explícito, puertos distintos y volúmenes nuevos. README explica esa protección. La recuperación/validación se diferencia de un cambio funcional: no se alteraron migraciones/SP para resolverla. No se reescribió Git ni se borraron bases para ocultar resultados.

### Configuración Docker/CI y límites de evidencia

Compose conserva db/dev; api es un perfil opcional. Publicación loopback: SQL1433, dev8080, api8081 por defecto. Healthcheck SQL precede dependientes, datos en volumen, scripts/database montados sólo lectura. Aprovisionar sólo bases inexistentes y separadamente de DbUp evita crear bases en migraciones. La configuración usa variables de entorno y mantiene Enlist=true; el guard deT10.2 no cambia.

CI .github/workflows/ci.yml preparado para Ubuntu/Docker, proyecto por run/attempt, contraseña efímera enmascarada y SQL real. Incluye tool restore/restore/Release build, suite completa sin omitir integración, auditoría JSON con rechazo de paquetes vulnerables y publish. Cleanup de volúmenes sólo en el runner efímero de ese proyecto. **No se ejecutó en GitHub**: estado remoto NOT VERIFIED, no CI verde inventada. No se instaló un servicio de revisión ni se envió código a terceros; sólo fuentes de paquetes/imágenes públicas necesarias para la ejecución autorizada.

Inspección local no identificó secretos reales registrados; valores públicos del template permanecen en el enunciado/historia y fallback local por compatibilidad. .env está ignorado/excluido del contexto Docker. Los puertos loopback mitiganF12; configuración de producción/credenciales/permisos sigue fuera de certificación. M02 Azure conserva revisión estática, no despliegue real; M37 no tuvo comprobación de acceso anónimo. No se usaron credenciales/sesiones remotas ambientales.

### Matriz vigente de los37 requisitos obligatorios

Esta tabla actualiza estados sin sustituir la descripción/evidencia histórica de sección4. **33 PASS,0 PARTIAL,2 FAIL,2 NOT VERIFIED**. La mejora es M33, instrucciones completas. Ocho operaciones implementadas/documentadas no equivalen a ocho comportamientos semánticamente aprobados.

| ID | Estado vigente | Evidencia/limitación |
|---|---|---|
| M01 | PASS | Build/runtime .NET10 y SQL2022 de copia limpia |
| M02 | NOT VERIFIED | Sin instancia Azure autorizada ni ejecución cloud |
| M03 | PASS | Esquema y modelo Property validados |
| M04 | PASS | Unidades/tipos y consulta real |
| M05 | PASS | Tenant y unicidad email |
| M06 | PASS | Lease y tipos/fechas |
| M07 | PASS | Técnicos/catálogos |
| M08 | PASS | Solicitud persistida por HTTP |
| M09 | PASS | Visita persistida por HTTP |
| M10 | PASS | Trigger008 y regresiones SQL/xUnit |
| M11 | PASS | Contrato vigente y guard transaccional |
| M12 | PASS | Transiciones aprobadas/rechazos |
| M13 | PASS | Asignación a técnico activo |
| M14 | PASS | Cierre con visita y UTC |
| M15 | PASS | Importes válidos y negativos rechazados |
| M16 | PASS | Esquema/PK/FK/constraints validados |
| M17 | PASS | Ocho scripts desde base nueva y journal |
| M18 | PASS | Tres edificios, seed idempotente |
| M19 | PASS | Treinta unidades |
| M20 | PASS | Veinte inquilinos/contratos |
| M21 | PASS | Cinco técnicos, uno inactivo |
| M22 | PASS | Cien solicitudes/estados/visitas en referencia inicial |
| M23 | PASS | POST solicitud201 real y pruebas |
| M24 | PASS | POST visita201/409 real y pruebas |
| M25 | PASS | PATCH200 y pruebas |
| M26 | PASS | GET unidades200 y pruebas |
| M27 | PASS | GET filtros/paginación200 y pruebas |
| M28 | PASS | GET historial200, costo12.34 verificado |
| M29 | FAIL | Resumen: indicadores manuales aún incorrectos |
| M30 | FAIL | Técnicos: indicadores/universo/orden aún incorrectos |
| M31 | PASS | SP de autoría manual declarada preservados |
| M32 | PASS | ProblemDetails y códigos de error probados |
| M33 | PASS | README completo y comandos ejercitados desde copia limpia |
| M34 | PASS | Decisiones consolidadas y historia conservada |
| M35 | PASS | Uso de IA transparente, sin validación humana inventada |
| M36 | PASS | Quince commits locales previos; no commits del agente |
| M37 | NOT VERIFIED | Falta acceso público anónimo/entrega real |

Extras opcionales: O01/O02/O03/O04/O05 PASS en el alcance descrito; O06 PARTIAL, workflow preparado y pasos locales verificados, ejecución GitHub pendiente. Ningún porcentaje de cobertura nuevo se reclama enT11;77.12% sigue siendo la medición histórica deT10.1.

### Hallazgos al cerrar T11

| ID | Estado | Evidencia o pendiente |
|---|---|---|
| F01 | OPEN | SP manuales preservados, poblaciones incorrectas |
| F02 | OPEN | Promedio entero preservado |
| F03 | OPEN | Universo/atribución/costos de técnicos sin corregir |
| F04 | OPEN | Fan-out/orden manual sin corregir |
| F05 | OPEN | Referencia temporal y validación SQL pendientes |
| F06 | RESOLVED | README de ejecución completo probado, enunciado archivado íntegro |
| F07 | RESOLVED |008 y regresiones vigentes |
| F08 | RESOLVED | Validación de ambos journals vigente |
| F09 | OPEN | Oracle de reportes no modificado para ocultar discrepancias |
| F10 | RESOLVED | Guard Enlist vigente |
| F11 | OPEN | Retención de bases y setup parcial permanecen documentados |
| F12 | DEFERRED | Mitigación local loopback/variables/CI efímera; producción no certificada |
| F13 | OPEN | Logging genérico de operación no cambiado por T11 |
| F14 | RESOLVED | Estado vigente al inicio de docs y snapshots históricos diferenciados |

Totales **5 RESOLVED,8 OPEN,1 DEFERRED**. No se reclasifica la deuda SQL como resuelta ni se marca T10 completo funcionalmente.

### Preservación y evaluación final

README original SHA256 archivado: A93C0A5F055061D6BE1F3B3D1705A67BA9D625B012B8C18991558B902E47634A. Blob Git normalizado original/archivo607f9a77a234003c4ff414e45facdfbc7a44fe10, idéntico. Los dos SP mantienen los hashes iniciales E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F y FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5; sus recursos006/007 y ValidateReports no cambiaron.

**Evaluación: CONDITIONAL.** El evaluador dispone de ejecución local reproducible, ocho endpoints, pruebas SQL reales y documentación; dos reportes incumplen requisitos obligatorios y faltan pruebas Azure/publicación/CI remota. No corresponde READY ni afirmar una aprobación de GitHub. El desarrollador debe decidir/corregir personalmente SQL, revisar diff, efectuar su commit/push/merge autorizados y comprobar Actions y acceso anónimo. El agente no ejecutó commit, push, merge, cambio de visibilidad, tags, releases o publicación remota. No se contrataron recursos Azure ni se inicia otra tarea.
