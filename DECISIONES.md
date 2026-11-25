# Decisiones técnicas

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
