# Decisiones técnicas

## T09: integración autorizada sin modificar SQL manual

Evidencia final dentro de dev: restore y build código 0; build 0 advertencias/errores. Suite completa: 87 unitarias y 144 de integración aprobadas, 0 fallidas y 0 omitidas (231 total; 19 nuevas T09 y las 212 anteriores vigentes). Auditoría de dependencias sin vulnerabilidades reportadas; publish código 0. git diff --check sin errores. SQL posterior: 3 edificios, 30 unidades, 20 inquilinos, 26 contratos, 5 técnicos, 100 solicitudes, 110 visitas; cero fixtures T09. Instancia temporal 8081 detenida; API existente 8080 intacta. La base principal de desarrollo no fue usada para escrituras. Se conservaron cuatro bases nuevas aisladas T04_T09_Clean/Existing de las dos ejecuciones de pruebas de migración, sin eliminar ninguna base existente.

Aceptación: integración C#/HTTP/DI/OpenAPI/DbUp, seguridad de parámetros, tipos, recursos publicados, journal/idempotencia y regresiones verificadas. Exactitud SQL original: no cumplida completamente, con 6 controles aprobados y 9 fallidos en ValidateReports (código 1 intencional como evidencia, no una validación verde). No todos los 29 casos originales se automatizaron; la matriz agrupada cubre 15 controles bajo el alcance posterior aceptado. Riesgo de uso: indicadores pueden presentar las discrepancias enumeradas abajo. Risk: none para la integración (lecturas nuevas, sin modificar SQL manual, datos existentes o contratos previos). No commit ni push.

Comprobación final de ejecución: se detectó una advertencia EF por tipo decimal implícito en resultados no mapeados. Se declaró Column(TypeName="decimal(38,2)") en ambos resultados SQL y en la sonda de prueba, sin tocar SPs. Suite completa repetida tras ese ajuste: 87/144 aprobadas, cero fallidas/omitidas. La evaluación adicional de revisión clasificó el texto EXEC como señal process_boundary/shell_process; no se interpretó esa clasificación como ejecución de un shell ni como aprobación de revisión. La revisión adicional está separada de las verificaciones funcionales registradas.

- Ambos procedimientos fueron escritos personalmente por el desarrollador y registrados previamente en Git. El responsable corrigió PropertiesSummary antes de integrar. Codex realizó revisión técnica y recibió esta instrucción posterior: "no, adelante con que lo que ya tenemos las prueba tencnica especifica que yo haga manualmente los sp se que hay deuda tecnica pero por el tiempo no nesitan ser prefectos solo poner en evidencia que se sql adelante con la implementacion prohibido tocar los sps". Se autoriza integrar las versiones existentes con deuda; no corregirlas ni declarar conformidad SQL completa.
- Estado inicial: modificación del usuario en database/scripts/dbo.usp_GetPropertiesSummary.sql, conservada. Hashes SHA256 antes/después: Properties E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F; Technicians FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5. Prueba automatizada compara bytes canónicos/embebidos. Ningún SP fue modificado por el agente.
- Ubicación canónica única en database/scripts. El csproj embebe bajo nombres lógicos 006/007 del prefijo existente. Sin copias, ejecutor adicional, modificación de 001–005 ni nuevas dependencias. DbUp comprobado desde base limpia y desde base con 001–005 realmente aplicadas; siete registros de journal y segunda ejecución sin cambios. Bases nuevas de pruebas conservadas, sin eliminar bases existentes.
- ReportRepository usa EF Core Database.SqlQuery<T>, SqlParameter explícitos y ToListAsync directo sobre EXEC. Sin composición, agregaciones C#, Dapper o DbSets. ReportService valida fechas HTTP y captura UTC con TimeProvider; DTOs y modelos separados. El SP actual ignora AsOfUtc: enviarlo no garantiza determinismo del cálculo.
- Metadatos contrastados con sys.dm_exec_describe_first_result_set: promedio INT nullable, costos DECIMAL(38,2), nullable sólo en técnicos; nombres NVARCHAR(200), IDs/conteos INT. Convertir promedio a decimal no recupera fracciones SQL. Un importe DECIMAL(38,2) no representable produjo OverflowException real; no se recorta ni redondea silenciosamente. to=9999-12-31 devuelve 400 por el DATEADD del SP; fechas mínimas válidas pasan.
- Deuda preservada: ClosedRequests/promedio pueden incluir cierres fuera del rango por CreatedAt; visitas sobre solicitudes ajenas a BASE se omiten; promedio pierde fracciones; SQL no valida rangos invertidos. Técnicos sin pendientes desaparecen; visitas de otros técnicos y solicitudes cerradas quedan fuera; visitas múltiples multiplican conteos/orden; GETDATE() ignora AsOfUtc y no limita eventos posteriores; costo sin visitas puede ser NULL. La carga considera Assigned/InProgress según lo confirmado por el responsable; los demás hallazgos siguen visibles.
- RED observado: ocho casos nuevos devolvían 404 antes de implementar; luego pasaron. Durante desarrollo se corrigieron dos errores de compilación en tests y una comparación LINQ no traducible, sustituida sólo en pruebas por SQL independiente. El primer ValidateReports falló por literales horarios incompletos del fixture; se corrigió ese archivo, sin tocar SPs.
- ValidateReports real: 15 controles, 6 PASS y 9 fallos de especificación identificados como deuda; error 51910, sqlcmd código 1 tras rollback. Incluye límites de día, cierres dentro/fuera, costos, cancelación, promedio, edificio vacío, técnicos activos/inactivos/ociosos, visitante y multiplicación por visitas. No se presenta como aprobación completa de los 29 escenarios originales.
- HTTP real desde Windows, instancia temporal 8081 sobre base aislada T04_Integration: ambos reportes 200. Rango 2026-05-01 a 2026-11-26: edificios 1/2/3, OpenRequests 31/24/0, ClosedRequests 19/16/0, promedio 12/12/null y costos 3285.00/2930.00/0.00. Workload: técnicos 1/2/3 con carga 14/13/13; el activo sin carga no aparece por el SQL preservado. OpenAPI 3.1.1: ocho operaciones; Swagger HTTP 200, sin afirmar validación visual.
- Auditoría de dependencias directas/transitivas contra nuget.org: ningún paquete vulnerable en los tres proyectos. Publish en directorio temporal del contenedor: código 0. Uso efectivo de IA: revisión/consulta de documentación EF, integración C#, fixtures, pruebas, documentación y análisis de ejecuciones reales. No se atribuye autoría del SQL manual a Codex ni se inventan validaciones humanas. No commit ni push; T10 no iniciada.

## T08: consultas, filtros, paginación e historial

Evidencia final: `docker compose exec -T dev dotnet restore PropertyMaintenance.slnx` y `docker compose exec -T dev dotnet build PropertyMaintenance.slnx` finalizaron con código 0; build con 0 advertencias y 0 errores. `docker compose exec -T -e PROPERTY_MAINTENANCE_TEST_CONNECTION=<conexión local aislada> dev dotnet test PropertyMaintenance.slnx` terminó con código 0: 84 unitarias y 128 de integración aprobadas, cero fallidas y omitidas (212 total). T08 añade 20 unitarias y 38 de integración; las 154 anteriores continúan aprobadas. Auditoría SQL posterior: 3 edificios, 30 unidades, 20 inquilinos, 26 contratos, 5 técnicos, 100 solicitudes y 110 visitas; cero fixtures T08 en edificios, inquilinos y solicitudes. Sólo se utilizó la base aislada para las pruebas; la base principal de desarrollo no recibió escrituras. README, compose y scripts SQL permanecen intactos. Risk: none (nuevas lecturas sin alterar contratos o reglas de escritura existentes). No commit ni push.

- Git limpio al comenzar; se conservaron arquitectura V2, DbUp, esquema, entidades generadas y endpoints de escritura. No se agregaron paquetes, migraciones ni reportes. Nuevos PropertyService/TenantService y repositorio específico de propiedades; ampliación mínima del repositorio de inquilinos y solicitudes. Los DTOs HTTP se mapean explícitamente desde modelos de lectura, sin exponer EF.
- RequestFilter centraliza validación de rangos, enums nombrados y límites. Query strings no utilizan el converter JSON: se comparan nombres de catálogo ignorando mayúsculas, rechazando números y valores vacíos. Offset en long y retorno vacío cuando supera CountAsync evitan conversión insegura a Skip(int). TotalPages usa aritmética entera con intermediario long.
- from/to representan días UTC, con límite superior exclusivo del día siguiente. to=9999-12-31 omite el límite superior porque SQL no puede almacenar un evento posterior; from=0001-01-01 es válido para DATETIME2. No se transforma la columna CreatedAt. Vigencia de contratos inclusiva con una fecha UTC capturada por TimeProvider; eventos leídos se presentan con sufijo Z según la convención UTC existente.
- AsNoTracking y proyecciones Select ejecutadas en SQL; LEFT JOIN conserva unidades vacantes y solicitudes sin técnico. Dos consultas para unidades, dos para página no vacía y tres para historial; ninguna consulta por fila. Historial por TenantId, con contratos y solicitudes independientes; SUM correlacionado por RequestId y COALESCE previenen duplicación y representan cero sin visitas.
- No se garantiza snapshot entre count/items o entre colecciones del historial. Offset puede desplazar/repetir resultados entre peticiones ante cambios concurrentes, aun con orden CreatedAt/RequestId determinista. No se introducen bloqueos o infraestructura para imponer una consistencia no solicitada.
- Se revisaron índices existentes de Units/Leases/MaintenanceRequests/Visits; no se agregaron sobre el pequeño seed. Un índice global CreatedAt/RequestId sólo se recomienda si mediciones y planes con volumen real lo justifican. No se afirma haber medido planes de ejecución ni realizado benchmark.
- Pruebas nuevas controlan UTC y fixtures aislados; verifican tenant vigente, contratos históricos/futuros, costos 10.10+20.25=30.35 sin multiplicación por tres contratos, costo cero sin visitas, filtros combinados, extremos, paginación estable y errores ProblemDetails. Comparaciones ADO.NET parametrizadas independientes verifican conteo global, IDs/orden del rango diario y costos por solicitud. La inspección real de comandos SQL verifica LEFT JOIN, OFFSET/FETCH, filtros sobre CreatedAt, SUM/COALESCE y número fijo de comandos. Los dos casos focalizados finales (SQL y último milisegundo del día) pasaron.
- HTTP real desde Windows a instancia temporal 8081 sobre PropertyMaintenanceDb_T04_Integration: GET properties/2/units devolvió 200, 10 unidades, 8 ocupadas y 2 vacantes; primera unidad 11, tenant 11. GET maintenance-requests?page=1&pageSize=2&status=Open&propertyId=1 devolvió 200, totalCount=10, totalPages=5, IDs 1 y 2 sin técnico. GET tenants/1/history devolvió 200, dos contratos y seis solicitudes; solicitud 1 con costo 0.00. OpenAPI 3.1.1 incluye las tres operaciones GET y Swagger UI respondió 200. No se afirma validación visual del navegador. No se detuvo la API existente en 8080.
- Uso efectivo de IA: inspección de implementación/SQL/docs, consulta de documentación oficial EF, generación de consultas y pruebas, ejecución real en Docker y análisis de SQL/HTTP. No se inventan validaciones humanas. Resultados de la suite final y limpieza registrados arriba. No commit ni push; T09 no iniciada.

## T07: registro de visitas

Evidencia final en dev: `dotnet restore PropertyMaintenance.slnx`, build y test finalizaron con código 0. Build: cero advertencias y errores. Suite completa: 64 unitarias y 90 de integración aprobadas, 0 fallidas, 0 omitidas (154 total); las 108 anteriores siguen pasando. La revisión estática independiente no encontró defectos graves; no sustituyó las ejecuciones. Auditoría final en la base aislada: 100 solicitudes, 110 visitas, 5 técnicos y cero residuos T07. README, compose y migraciones sin cambios; git diff --check sin errores.

IDs actuales consultados sin modificar PropertyMaintenanceDb_T03_Demo: solicitudes 16/17 Assigned y 36/37 InProgress. Técnicos 1–4 activos; 5 inactivo. Para probar un técnico diferente al asignado usar solicitud 16 con técnico 2 o solicitud 36 con técnico 1. Costo cero es válido. Reiniciar la API para cargar T07; se conserva Swagger en /swagger/index.html, OpenAPI 3.1 y la corrección de parámetros de ruta.

- Git limpio al comenzar. Se reutilizaron repositorios de solicitudes/técnicos, TimeProvider, ProblemDetails, OpenAPI y fixtures de pruebas. VisitService específico, VisitModel, repositorio mínimo y mapeo explícito evitan agregar reglas de visitas al Controller o ampliar innecesariamente el servicio de transiciones. No se agregaron dependencias ni migraciones.
- Notes: SQL NVARCHAR(MAX) nullable; el contrato impone obligatoriedad y máximo 2000. Cost: decimal, máximo 9999999999999999.99, escala dos, incluyendo cero. Cost nullable en DTO distingue omisión/null; un converter específico revisa el texto numérico antes de TryGetDecimal para evitar que 1e-100 u otra entrada extrema se redondee silenciosamente. Se admite notación científica representable. Escala efectiva mayor a dos se rechaza incluso con ceros adicionales; no se usa double/float.
- Transacción VisitService ReadCommitted: bloqueo padre UPDLOCK,HOLDLOCK → lectura de estado → técnico HOLDLOCK → hora UTC truncada a milisegundos → INSERT → commit. T06 añade únicamente el mismo bloqueo padre después de su lectura original y antes de validaciones dependientes, conservando RowVersion. Cambiar el orden previene inversión con bloqueos de visitas; no cambia reglas ni incorpora reintentos.
- Las nuevas operaciones no alteran StatusId/TechnicianId/RowVersion del padre. El registro puede realizarlo cualquier técnico activo. Una excepción después de SaveChanges revierte la visita; no se confirma parcialmente. El handler añade conflictType para distinguir RowVersion, deadlock y timeout, conservando los códigos HTTP existentes.
- Concurrencia real: A (Assigned/cancelación) y B (InProgress/cierre con visita previa) usaron dos solicitudes HTTP con conexiones/transacciones diferentes, verificadas por SPID. La visita pausó tras validar; se observó la sesión de transición bloqueada por ella mediante DMV con wait_type LCK_M_*. Tras liberar, ambos commits fueron válidos (visita antes del terminal); una nueva visita terminal recibió 409 sin INSERT.
- Escenario C: técnico temporal activo, visita pausada y UPDATE de desactivación en otro contexto/transacción. Se observó bloqueo real; la visita confirmó primero y después la desactivación. Intento posterior recibió 409 TECHNICIAN_INACTIVE. Se eliminó únicamente el técnico temporal después de limpiar la visita. No se modificaron técnicos del seed. En esos escenarios no se observaron deadlocks ni excepciones de timeout; se observaron esperas normales que terminaron al liberar el gate. Una falla inyectada después del INSERT produjo 500 y rollback verificado.
- Flujo HTTP real desde Windows, instancia temporal 8081 y base aislada PropertyMaintenanceDb_T04_Integration: solicitud 311 creada Open (201), asignada a técnico 1 (200), InProgress (200), visita 131 por técnico 2 por 12.34 (201), Closed (200), nueva visita rechazada (409). SQL confirmó exactamente una visita, sin cambiar el técnico asignado. CreatedAt 2026-11-25T14:05:00.200Z, VisitedAt 14:05:01.317Z y ClosedAt 14:05:01.376Z. Se limpiaron exclusivamente visita 131 y solicitud 311 tras auditar. El POST retornó notes recortadas y no generó Location.
- Uso efectivo de IA: inspección de SQL/C#, documentación EF, implementación y ejecución de pruebas unitarias/HTTP/SQL con análisis de bloqueos. No se afirma validación humana. Risk: item 4 (concurrencia y orden de bloqueos). No commit ni push; T08 no iniciada.

## Corrección de compatibilidad de Swagger UI en T06

Corrección posterior verificada: publicar OpenAPI 3.0 produjo versión 3.0.4, que el bundle instalado rechaza mediante su regex de versiones 3.0.0–3.0.3. Se detectó inspeccionando el JavaScript servido, no por ausencia real del campo version (el documento retornaba HTTP 200). Se restaura explícitamente OpenAPI 3.1 y se conserva el transformer de ruta int32. La prueba de regresión exige ahora versión 3.1.x además de id integer. Suite final: 47 unitarias y 61 de integración aprobadas, 0 fallidas/omitidas. Instancia temporal en 8081 para comprobar documento/JS sin detener la API del usuario. El acceso automatizado al navegador no estuvo disponible; no se afirma validación visual. La evidencia de OpenAPI 3.0 anterior queda reemplazada por esta decisión.

- Reporte real: Swagger UI rechaza el parámetro id como ausente pese a haberse ingresado 101. El documento servido en localhost:8080 era OpenAPI 3.1.1 con schema.type=[integer,string] para id; el controlador recibe int. La prueba de regresión reprodujo esa unión.
- Se conserva el generador nativo y la dependencia UI existente. Se publica OpenAPI 3.0 para compatibilidad y un transformer puntual normaliza parámetros de ruta int32 a type=integer, manteniendo required, rango y formato. Cambiar sólo la versión del documento no bastó: la prueba detectó ausencia de type después de serializar la unión. No se cambió el comportamiento JSON, las reglas de negocio ni los endpoints para arreglar la documentación.
- La prueba SwaggerPathIdUsesSingleIntegerType verifica el documento HTTP real de WebApplicationFactory, nombre/ubicación/required y tipo integer del parámetro. Se debe reiniciar la API y refrescar Swagger para consumir el documento nuevo. No se afirma una validación visual del navegador que no se haya ejecutado.
- Resultado final real en dev: 47 unitarias y 61 de integración aprobadas, cero fallidas y omitidas; compilación/restauración realizadas por dotnet test sin errores. La prueba nueva pasó después de reproducir el fallo original y verificar la corrección.
- Uso de IA: inspección del documento activo, consulta de documentación oficial, corrección y ejecución real de pruebas. No commit ni push.

## T06: transiciones de estado

Evidencia final en dev: restore y build código 0, cero advertencias/errores; suite completa 47 unitarias y 60 de integración aprobadas, 0 fallidas, 0 omitidas (107 en total). Incluye las 48 pruebas anteriores, 25 pares parametrizados, exactamente cinco permitidos, actividad/visitas/fechas/no persistencia y HTTP de concurrencia independiente. La revisión estática independiente no encontró defectos graves y no sustituyó la ejecución. Se verificó también ID de ruta no numérico como 400. Auditoría posterior: 100 solicitudes originales y cero residuos T06 en solicitudes/visitas. Se detuvo únicamente el contenedor temporal de la prueba HTTP; la API que ocupaba 8080 no se alteró. `git diff --check` sin errores; README, compose y migraciones sin cambios.

- Se verificó Git limpio al iniciar. Se reutilizaron modelo, servicio, repositorios, Response DTO, serialización textual y ProblemDetails. No se añadieron paquetes ni se modificaron migraciones. La ampliación del constructor del servicio exigió adaptar los dobles de T05, sin cambiar sus aserciones ni comportamiento.
- Reglas en CanTransition mediante switch: cinco pares permitidos y veinte rechazados, incluyendo mismo estado y terminales. Assigned exige técnico activo existente; se conserva en los destinos posteriores sin revalidar actividad ni especialidad. Closed exige visita; Cancelled no. Fecha terminal de TimeProvider, UTC truncada a milisegundos y validada contra CreatedAt.
- Transacción nativa ReadCommitted: lectura original de solicitud sin seguimiento, consultas puntuales HOLDLOCK de técnico/visitas y UPDATE por ID más RowVersion original. Se evita Serializable sobre toda la operación. El repositorio no relee/reemplaza la versión ni modifica otros campos. DbUpdateConcurrencyException se distingue de timeout/deadlock y devuelve CONCURRENCY_CONFLICT; no hay retry automático ni token HTTP.
- Prueba concurrente real: dos contextos, dos transacciones y sesiones SQL con @@SPID distintos. La transacción ganadora confirma Cancelled y cambia RowVersion; la HTTP perdedora intenta Assigned con la versión original y recibe exactamente DbUpdateConcurrencyException y HTTP 409. Se verifica que conserva la cancelación ganadora. No se acepta una excepción SQL genérica como prueba de conflicto optimista.
- El primer intento de ese fixture falló porque @@SPID devuelve SMALLINT y se había solicitado int sin conversión. Se corrigió sólo el probe con CONVERT(int,@@SPID) y la repetición focalizada pasó.
- Ejecución real HTTP desde Windows sobre instancia temporal en localhost:8081 (8080 estaba ocupado, no se detuvo la API existente): solicitud temporal 144 recorrió Assigned → InProgress → Closed, 145 Open → Cancelled y 146 Assigned → Cancelled. Las cinco respuestas fueron 200. SQL confirmó estados finales 4/5/5, conservación de técnico 1/NULL/1 y ClosedAt. Se preparó una visita SQL temporal para cerrar 144; luego se eliminaron exclusivamente los tres fixtures y su visita.
- Ejemplo observado de cierre: `PATCH /api/maintenance-requests/144/status` con `{"status":"Closed"}` devolvió status Closed, technicianId 1, createdAt 2026-11-24T12:50:46.449Z y closedAt 2026-11-25T12:51:11.37Z. Ese ID temporal ya no existe.
- IDs del seed intactos comprobados en la base aislada PropertyMaintenanceDb_T04_Integration: 1 Open para asignar a técnico activo 1; 2 Open para cancelar; 16 Assigned para iniciar; 17 Assigned para cancelar; 36 InProgress con dos visitas para cerrar. Cada operación cambia los datos: usar cada ejemplo una sola vez y no asumir IDs equivalentes en otra base. Swagger conserva /swagger/index.html; el documento incluye PATCH. Reiniciar la API para cargar T06.
- Consulta final de sólo lectura confirmó los mismos cinco IDs/estados y técnico 1 activo en PropertyMaintenanceDb_T03_Demo, la base del comando de inicio manual. No se modificó esa base para validar T06.
- Riesgo pendiente T07: una visita no incrementa automáticamente RowVersion de la solicitud. Su futura escritura debe bloquear la fila padre y validar estado dentro de la misma transacción, coordinándose con el UPDATE terminal. Bloqueos y orden de acceso deberán revisarse; no se implementó T07. Risk: item 4 (concurrencia).
- Uso efectivo de IA: consulta de documentación EF, implementación, pruebas y análisis de resultados SQL/HTTP; no se afirma validación humana. No commit ni push.

## T05: creación de solicitud

Evidencia final: restore y build dentro de dev código 0, cero advertencias y cero errores. `dotnet test PropertyMaintenance.slnx --logger "console;verbosity=minimal"` con conexión aislada: 10 pruebas unitarias aprobadas y 38 de integración aprobadas (15 de T04, 21 HTTP y 2 de concurrencia/rollback), 0 fallidas y 0 omitidas. Se verifican también prioridad numérica definida rechazada, respuesta 500 sin diagnóstico interno y campos controlados por servidor rechazados. `git diff --check` sin errores; README, compose y migraciones previamente aplicadas sin cambios.

Auditoría final: 100 solicitudes de seed y cero registros temporales T05/HTTP residuales. `dotnet list PropertyMaintenance.slnx package --vulnerable --include-transitive` finalizó con código 0 sin vulnerabilidades conocidas en los tres proyectos según nuget.org. La revisión estática independiente no encontró defectos graves; no sustituyó las ejecuciones SQL/HTTP realizadas.

- Se preservaron los cambios pendientes de PKCS de T04. El alcance nuevo prevalece sobre la antigua descripción T05 del backlog; no se implementaron CRUD completos ni T06.
- Se reutilizan repositorios/modelo/enums de T04 y se agregan sólo Exists de Unit y Tenant. Controller → Service → Repository; DTO de entrada restringido y respuesta explícita. Estado inicial SQL Open; campos de técnico/cierre ausentes del alta. Se rechazan miembros JSON desconocidos para impedir overposting. Enums HTTP como texto, sin números.
- Longitudes contrastadas con 002: Category NVARCHAR(100); Description NVARCHAR(MAX), restringida a 2000 por contrato HTTP/servicio sin alterar SQL. Espacios exteriores recortados; vacíos rechazados.
- TransactionScope Serializable nativa, async, 30s y un contexto Scoped compartido. SaveChanges permanece en repositorio; la confirmación la controla el servicio. Una fecha UTC única de TimeProvider, truncada a milisegundos antes de calcular el día, evita que validación y CreatedAt difieran en medianoche. AddAsync ahora persiste esa fecha si viene informada; DateTime default conserva el default SQL y las pruebas T04.
- Dependencias añadidas: Swashbuckle.AspNetCore.SwaggerUI 7.2.0 únicamente para UI y Microsoft.AspNetCore.Mvc.Testing 10.0.15 sólo en IntegrationTests. Se mantiene OpenAPI nativo y la corrección Pkcs 9.0.21; no se agregó mocking framework.
- Pruebas SQL reales sobre PropertyMaintenanceDb_T04_Integration (base aislada): el segundo cliente físico intenta terminar un contrato entre validación e inserción; obtiene error SQL exacto 1222, mientras creación confirma. Auditoría confirma contrato intacto y solicitud persistida, luego limpia sólo el fixture. Falla inyectada después de SaveChanges revierte por completo la inserción. No se deshabilitó el trigger.
- POST real desde Windows a localhost:8080 respondió 201 con RequestId 110, UnitId/TenantId 1, prioridad High, estado Open, técnico/cierre NULL, Category Plumbing, Description T05 manual HTTP probe y CreatedAt 2026-11-25T12:02:16.042Z. SQL confirmó estado 1 y RowVersion de 8 bytes. Se eliminó únicamente esa solicitud temporal identificada. Swagger y OpenAPI devolvieron 200.
- Uso efectivo de IA: documentación oficial, implementación, pruebas y análisis de bloqueos/resultados. No se afirma validación humana. Limitaciones: no retry automático, no transacciones distribuidas, no prohibición nueva de cambios históricos posteriores al commit. Risk: item 4 (bloqueos concurrentes) e item 5 (dependencias). No commit ni push.

### Ejecución T05

En PowerShell, configurar por entorno la conexión local ya aprovisionada (en Docker Server=db,1433) y ejecutar desde la raíz:

```powershell
docker compose exec -T -e ASPNETCORE_ENVIRONMENT=Development -e "ConnectionStrings__PropertyMaintenance=$env:ConnectionStrings__PropertyMaintenance" dev dotnet run --project src/PropertyMaintenance.Api --no-launch-profile --urls http://0.0.0.0:8080
```

Para pruebas, PROPERTY_MAINTENANCE_TEST_CONNECTION debe apuntar a una base local cuyo nombre comience por PropertyMaintenanceDb_T04_; la fixture aplica DbUp/seed y las mutaciones se revierten o limpian por ID. Ejecutar:

```powershell
docker compose exec -T -e "PROPERTY_MAINTENANCE_TEST_CONNECTION=$env:PROPERTY_MAINTENANCE_TEST_CONNECTION" dev dotnet test PropertyMaintenance.slnx
```

Swagger: http://localhost:8080/swagger/index.html; documento: http://localhost:8080/openapi/v1.json. IDs comprobados en la base aislada: unidad/inquilino 1/1 y 3/3 con contrato sin fin, 2/2 con fin informado. No suponer estos valores en otras bases: consultar Leases/Units/Tenants. Payload ejemplo: `{"unitId":1,"tenantId":1,"category":"Plumbing","priority":"High","description":"Demo leak"}`.

## T04: corrección de seguridad de PKCS

- Causa verificada con `dotnet nuget why`: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.7 y `DbUp` 5.0.40 (por dbup-sqlserver) convergen en `Microsoft.Data.SqlClient` 6.1.1, que introduce `System.Security.Cryptography.Pkcs` 9.0.4. La referencia al proyecto API propaga la dependencia a ambos proyectos de pruebas.
- Avisos oficiales consultados: [GHSA-2fmv-rv2c-59x7 / CVE-2026-70162](https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7), severidad moderada (NU1902), y [GHSA-846f-f764-76mx / CVE-2026-84008](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx), severidad alta (NU1903). Ambos describen denegación de servicio. Afectan 9.0.0–9.0.20; primera versión corregida en esa rama: 9.0.21. También publican correcciones 8.0.2 y 10.0.13; no se requiere cambiar de rama para resolver estos avisos.
- Solución mínima: referencia explícita a `System.Security.Cryptography.Pkcs` 9.0.21 en API, con comentario de seguridad. La versión está publicada en NuGet y es compatible con net10.0. Se conservan EF Core/Design/dotnet-ef 10.0.7, SqlClient 6.1.1 y DbUp 5.0.40. No se actualizaron indiscriminadamente otros paquetes ni se usó NoWarn o se deshabilitó NuGet Audit. El override podrá retirarse cuando la dependencia ascendente resuelva por sí misma una versión segura.
- Verificación real dentro del contenedor dev: `dotnet restore PropertyMaintenance.slnx --force-evaluate` código 0, sin advertencias; `dotnet build PropertyMaintenance.slnx --no-restore` código 0, cero advertencias y cero errores; `dotnet list PropertyMaintenance.slnx package --vulnerable --include-transitive` código 0, sin paquetes vulnerables conocidos según nuget.org en los tres proyectos; `dotnet nuget why` confirma 9.0.21 en ambas rutas transitivas.
- `dotnet test PropertyMaintenance.slnx --no-build --no-restore --logger "console;verbosity=normal"`, con `PROPERTY_MAINTENANCE_TEST_CONNECTION` apuntando a `PropertyMaintenanceDb_T04_Integration`: código 0, 15 aprobadas, 0 fallidas, 0 omitidas. UnitTests continúa sin casos. Las pruebas usan SQL Server local aislado y rollback.
- Las advertencias descritas en la evidencia anterior de T04 son históricas y quedan resueltas por esta corrección. La auditoría expresa el estado conocido por las fuentes consultadas, no una garantía sobre vulnerabilidades futuras.
- Uso efectivo de IA: trazado de dependencias, consulta de avisos oficiales y versión publicada, parche mínimo y ejecución de verificaciones. Alcance limitado a T04; no se implementó T05 ni se ejecutaron commits o push. Risk: item 5 (dependencia de runtime actualizada), mitigado con auditoría y pruebas reales.

## T04: acceso a datos Database First

Evidencia: tool restore, restore y build ejecutados dentro de dev con código 0. Ejecución final xUnit sobre `PropertyMaintenanceDb_T04_Integration`: 15 aprobadas, 0 fallidas, 0 omitidas; UnitTests sigue sin casos. Primera ejecución detectó dos errores de test (renovación del mismo inquilino y formato decimal normalizado); se corrigieron los fixtures/aserciones, no el esquema. Concurrencia: dos DbContext independientes sobre una conexión/transacción compartida para rollback, primer escritor genera nueva RowVersion y segundo recibe DbUpdateConcurrencyException exacta. UPDATE de Leases válido y rechazo INSERT solapado con SQL 51021 comprobados con trigger habilitado.

Limitación documentada: no se añadió operación de alta de Lease; recuperación de IDENTITY tras INSERT válido con INSTEAD OF sigue pendiente de una estrategia específica. Advertencias NU1902/NU1903 de System.Security.Cryptography.Pkcs 9.0.4 reportadas por restauración; no se agregaron paquetes fuera de los tres solicitados para resolverlas. Risk: item 5 (dependencias) e item 4 (verificación de concurrencia). No commit ni push.

- EF Core SqlServer, Design y dotnet-ef local alineados en 10.0.7 estable para net10.0. Design con PrivateAssets=all. No se instalaron herramientas globales.
- Scaffold real desde SQL Server local, base T03_Demo con cinco migraciones DbUp verificadas. Once tablas explícitas, sin journals ni OnConfiguring. SQL sigue siendo la fuente de verdad; migraciones existentes intactas.
- Contexto Scoped, conexión compartida con DbUp, inicialización previa a peticiones, sin creación de esquema por EF ni logging sensible.
- UseSqlOutputClause(false) y DeleteBehavior.NoAction en clase parcial, sin alterar salida generada. ROWVERSION generado/token de concurrencia; mapeo explícito clona bytes y devuelve fechas con Kind UTC. DATETIME2 no transporta zona horaria.
- Repositorios mínimos concretos: consulta AnyAsync de contrato inclusivo; consulta por ID AsNoTracking y alta de solicitud con SaveChanges atómico. Sin servicio, transiciones, endpoints ni repositorio genérico. AddAsync sólo recibe los campos de alta; identidad, fecha, estado inicial y RowVersion los genera SQL y se devuelven en un modelo nuevo.
- Regeneración mediante database/scaffold.sh, conexión externa y herramienta local; --force requiere revisar previamente la separación de archivos generados y personalizados.
- Uso efectivo de IA: consulta de documentación oficial, ejecución del scaffold, implementación y revisión de mapeos, repositorios, pruebas SQL reales y documentación. No se afirma validación humana.

## T03: seed y evidencia real

- Se reutilizó DbUp sin nuevas dependencias. Catálogos obligatorios en 005; demo embebida con journal `DemoSeedVersions` y transacción por script, habilitada explícitamente mediante configuración (false por defecto).
- Se exige una base sin registros de negocio para evitar mezclar fixtures con datos existentes. El journal proporciona idempotencia; no se utiliza la presencia superficial de filas como señal de éxito.
- Referencia única a medianoche UTC persistida en descripciones; identificadores resueltos por claves conocidas. No se modificaron migraciones 001–004 ni se deshabilitaron restricciones.
- En `PropertyMaintenanceDb_T03_Demo`: 3/30/20/26/5/100/110 filas en las siete tablas de negocio; contratos 18 vigentes, 6 históricos y 2 futuros; solicitudes 15/20/20/35/10. ValidateSeed pasó 17 controles.
- Segunda inicialización: cero scripts pendientes. Comparación íntegra de JSON ordenado de las siete tablas antes/después (52 701 caracteres, incluyendo fechas, identidades y RowVersion): idéntico.
- En `PropertyMaintenanceDb_T03_Empty`, seed deshabilitado: siete tablas de negocio vacías, catálogos 2/3/4/5 y cinco migraciones registradas.
- En `PropertyMaintenanceDb_T03_Failure`, un trigger temporal sobre Visits arrojó 51999: API abortó, cero filas de negocio y journal del seed ausente. Se retiró el trigger temporal. No se eliminaron bases.
- Restore/build/test en dev: código 0; build con 18 advertencias NU1901/NU1902 transitivas ya conocidas, cero errores. Test runner ejecutado sin pruebas C# reales. Los procesos de API exitosos se detuvieron con timeout tras observar arranque; su código 1 por timeout no se confunde con fallo de migración.
- Uso efectivo de IA: implementación de SQL/C#, validaciones y documentación; revisión del resultado y corrección del parseo de UTC y de los escenarios de cancelación. No se afirma revisión humana.
- Riesgos: ejecutar un único inicializador durante despliegue; fechas relativas a la referencia inicial, no se actualizan automáticamente. No se ejecutaron commit ni push. T04 permanece pendiente.

## Decisiones aprobadas

| Decisión | Justificación |
|---|---|
| Monolito ASP.NET Core en capas pragmáticas | El alcance de la prueba no justifica distribuir el dominio en múltiples proyectos; una única API con carpetas claras reduce complejidad y facilita mantenimiento y entrevista. |
| Tres proyectos | `Api` concentra la implementación; UnitTests e IntegrationTests mantienen separación de verificaciones y referencian únicamente `Api`, según T00 V2. |
| Controllers REST | Cumple el contrato de endpoints de la prueba y ofrece una frontera HTTP explícita y fácil de probar. |
| SQL Server como fuente de verdad | Las restricciones estructurales, trigger y reportes deben ser reproducibles en SQL Server. |
| EF Core Database First | Permite consumir el esquema aprobado sin convertir migraciones EF en autoridad del modelo. |
| DbUp para scripts versionados | Proporciona ejecución ordenada e idempotente de SQL versionado sin usar migraciones EF. |
| Repositorios específicos y mapeo explícito | Mantiene consultas defendibles, separa persistencia de modelos de negocio y evita abstracciones genéricas. |
| Swagger, Problem Details e ILogger | Cumplen los requisitos de operación, documentación y errores consistentes con componentes nativos. |
| Sin paquetes adicionales en T00 | No se agrega software de aplicación antes de una tarea que lo necesite; el esqueleto conserva únicamente dependencias de plantilla y pruebas. |

## Corrección V2

La arquitectura inicial propuesta con proyectos `Domain`, `Application` e `Infrastructure` fue reemplazada por la arquitectura de tres proyectos solicitada en la corrección T00 V2. La justificación aprobada es proporcionalidad con el alcance, simplicidad y facilidad de mantenimiento. Esta decisión prevalece.

## Fuera de alcance de T00

No se implementaron tablas SQL, scaffold EF, entidades de negocio, endpoints, repositorios, servicios, stored procedures, seed ni lógica de negocio.

## Registro inicial veraz de uso de IA

- Se utilizó un asistente de IA para inspeccionar el repositorio, contrastar `README.md`, `compose.yaml` y `.gitignore`, y generar el esqueleto documental y de proyectos solicitado.
- La arquitectura V2, el alcance y las restricciones fueron definidos por el responsable humano y se trataron como autoridad.
- La validación prevista para T00 es la ejecución real de `restore`, `build` y `test` dentro del contenedor `dev`; no se afirma una validación humana que no haya ocurrido.

## Evidencia de T00

- `restore`, `build` y `test` se ejecutaron dentro de `dev` con resultado final exitoso.
- El template .NET 10 requirió declarar `Microsoft.AspNetCore.OpenApi.Generated` en `InterceptorsNamespaces` para compilar la generación OpenAPI; se aplicó únicamente al proyecto API.
- La primera restauración paralela dejó archivos NuGet incompletos; se repararon solo los directorios dañados de `xunit` y `xunit.abstractions`, y la restauración secuencial posterior fue exitosa.
- No se ejecutaron commits ni push.

## Decisiones de T01

- Las migraciones no contienen `CREATE DATABASE` ni `USE`; la base se aprovisiona por separado y DbUp podrá ejecutar los scripts en el contexto que le corresponda.
- Los valores de catálogos fijos se insertan junto con sus tablas porque las FK y los identificadores aprobados requieren esos datos de referencia desde la primera instalación. El seed de negocio queda reservado para T03.
- Las restricciones únicas de `Units(PropertyId, Number)` y `Tenants(Email)` ya crean índices únicos, por lo que no se duplican como índices no únicos.
- Las reglas que requieren otras filas, concurrencia o comportamiento de aplicación no se simulan mediante checks: el trigger de contratos superpuestos queda explícitamente pendiente de T02.
- Las validaciones usan fixtures temporales en una transacción y verifican conteos después del rollback para demostrar que no dejan datos residuales.
- Resultado real de T01: las cinco ejecuciones de validación SQL terminaron con código 0 después de corregir la lectura de metadatos nativos de SQL Server; la base contiene 11 tablas y cero filas de negocio.
- La solución .NET se compiló con código 0, 0 advertencias y 0 errores; el test runner terminó con código 0 sin pruebas reales descubiertas.

## Registro de uso de IA en T01

- Se utilizó IA para convertir las especificaciones aprobadas en scripts T-SQL, validaciones de metadatos y casos transaccionales de prueba.
- Se verificaron los scripts contra el SQL Server local del contenedor `db`; los resultados reales se registran al cierre de T01.
- No se afirmó validación humana ni se incorporaron funcionalidades fuera de los entregables de T01.

## Decisiones de T02

- Se agregó `DbUp` 5.0.40 al proyecto API. Es la dependencia estable seleccionada para ejecutar scripts SQL Server sin introducir un proyecto adicional ni migraciones EF.
- Los scripts se embeben desde `database/migrations` con nombres lógicos `PropertyMaintenance.Api.database.migrations.<numero>_<nombre>.sql`. Los prefijos numéricos proporcionan orden determinista y el ensamblado publicado contiene los mismos recursos.
- Se usa `dbo.SchemaVersions` como journal y `WithTransactionPerScript()`. Así, un fallo revierte el script actual, conserva el historial real de scripts completados y aborta el arranque.
- No se adopta automáticamente la base T01 existente sin journal. La primera ejecución sobre una base limpia o aislada es el mecanismo de verificación; una base existente requiere recreación controlada o un procedimiento de baseline autorizado.
- El trigger usa `sys.sp_getapplock` exclusivo, propietario de la transacción, por `UnitId` afectado y en orden ascendente. Esto serializa operaciones incompatibles incluso cuando no existían filas previas y reduce deadlocks por orden inverso. La comprobación set-based detecta conflictos dentro del lote y contra filas existentes.
- El primer intento de DbUp reveló que `CREATE TRIGGER` debe ser la primera sentencia del batch; se eliminó el `SET NOCOUNT ON` externo y se conservó dentro del cuerpo del trigger. No se ocultó ni se marcó 004 como aplicada durante el fallo.

## Evidencia de T02

- Base limpia: cuatro scripts aplicados y cuatro entradas observadas en `dbo.SchemaVersions`.
- Segunda ejecución: cero scripts pendientes, sin reejecución.
- Base con fallo intermedio: journal conservó 001–003; la ejecución posterior aplicó únicamente 004.
- Validación de leases: 15 casos requeridos aprobados, incluido error fijo 51021 y rollback de fixtures.
- Concurrencia real: dos conexiones independientes; una sola lease incompatible confirmó, las compatibles confirmaron ambas y las unidades diferentes confirmaron ambas.
- Restore/build/test reales finalizaron con código 0. El restore/build reportó advertencias NU1901/NU1902 transitorias existentes en la cadena de dependencias; no se ocultaron.

## Registro de uso de IA en T02

- Se utilizó IA para consultar la documentación de DbUp, implementar el componente, el trigger y las validaciones, y revisar los resultados observados.
- La ejecución real se realizó contra SQL Server local en bases aisladas; los resultados y el fallo inicial del batch quedaron documentados.
- No se ejecutaron commits ni push.

CorrecciÃ³n verificada: el trigger quedÃ³ como `INSTEAD OF INSERT, UPDATE` para adquirir los applocks antes de la escritura base; las validaciones finales cubrieron los 15 casos de leases, las restricciones y tres pares de sesiones concurrentes sin deadlock.
