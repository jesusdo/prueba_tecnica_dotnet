# Decisiones técnicas

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
