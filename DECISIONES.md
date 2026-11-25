# Decisiones técnicas

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
