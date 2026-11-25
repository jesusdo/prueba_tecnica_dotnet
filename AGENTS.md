# AGENTS.md

## Alcance y autoridad

- El responsable humano define las decisiones técnicas y el alcance de cada tarea.
- Implementar únicamente especificaciones aprobadas; no modificar arquitectura ni agregar funcionalidades unilateralmente.
- T00 V2 adopta una arquitectura en capas pragmática dentro de un único proyecto ASP.NET Core.
- No crear proyectos `Domain`, `Application` o `Infrastructure`.
- No ejecutar `git commit` ni `git push` sin autorización explícita.

## Convenciones técnicas aprobadas

- C#, .NET 10, ASP.NET Core Controllers REST y SQL Server 2022.
- Persistencia con EF Core 10 Database First, SQL Server como fuente de verdad y scripts SQL versionados mediante DbUp.
- Flujo: `Controller -> Service -> Repository -> SQL Server`.
- Repositorios específicos; no repositorio genérico, MediatR, CQRS, AutoMapper ni abstracciones innecesarias.
- Separar modelos de negocio y persistencia mediante mapeo explícito.
- OpenAPI/Swagger UI, Problem Details, `ILogger` e inyección de dependencias nativa.
- xUnit para pruebas unitarias y de integración.
- Consultas parametrizadas y ausencia de secretos personales o corporativos.

## Estructura aprobada de `Api`

`Controllers`, `DTOs`, `Business/Services`, `Business/Models`, `Business/Enums`, `DataAccess/Entities`, `DataAccess/Repositories`, `DataAccess/Context`, `DataAccess/Migrations`, `Mappings` y `Exceptions`.

## Reglas de trabajo

- Leer primero el README y la configuración existente; conservar el enunciado original.
- Verificar las afirmaciones técnicas contra el código o la documentación disponible.
- Registrar decisiones, riesgos, supuestos y cualquier desviación.
- Ejecutar verificaciones proporcionales y reportar resultados reales; no inventar compilaciones ni pruebas.
- Mantener el código sencillo, legible y defendible en entrevista.
- Registrar en `DECISIONES.md` el uso efectivo de IA, sin inventar validaciones humanas.
- No implementar lógica de negocio, tablas SQL, scaffold, seed, stored procedures ni endpoints de negocio durante T00.
