# T09 reporting integration
Objective: expose the developer's manual SQL procedures without editing them. Branch: existing working branch; no commits or push.
Runner: Docker dev, xUnit and local isolated SQL Server. Route: inline implementation; bounded explorer for fixture mapping.

## Specs
S1. "prohibido tocar los sps"
S2. "Crear IReportRepository y ReportRepository." "Utilizar EF Core 10 y el DbContext existente." "No implementar nuevamente las agregaciones en LINQ o C#."
S3. "GET /api/reports/properties-summary?from=&to=" "GET /api/reports/technicians-workload"
S4. "Ambos parámetros son obligatorios." "Devolver 400 cuando los parámetros sean inválidos, falten o el rango esté invertido."
S5. "Obtener AsOfUtc mediante TimeProvider y enviarlo al procedimiento para permitir pruebas deterministas."
S6. "Aplicación desde una base limpia." "Segunda ejecución sin reaplicar migraciones." "Actualización de una base existente."
S7. "Auditoría de dependencias vulnerables." "No ejecutar commit ni push." "No avanzar a T10."
S8. "se que hay deuda tecnica pero por el tiempo no nesitan ser prefectos". Known SQL deviations must remain visible; passing integration checks must not be presented as full SQL specification compliance.

## Tasks
- T1 S1,S6 inline done: canonical bytes embedded under 006/007; both SHA256 hashes unchanged; no commit.
- T2 S2-S5 inline done: repository, models/DTOs, service/controller, DI; RED eight HTTP 404 failures, then GREEN; no commit.
- T3 S6-S8 inline done: SQL 6 PASS/9 known failures, HTTP and migration tests, real HTTP, full suite 231 pass, audit clear; no commit.
- T4 S1,S7,S8 inline done: five docs updated with actual evidence and original criteria not fully met; no commit.

## Log
L1. User override: "no, adelante con que lo que ya tenemos las prueba tencnica especifica que yo haga manualmente los sp se que hay deuda tecnica pero por el tiempo no nesitan ser prefectos solo poner en evidencia que se sql adelante con la implementacion prohibido tocar los sps"
L2. Existing user edit: database/scripts/dbo.usp_GetPropertiesSummary.sql. Hashes before integration: Properties E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F; Technicians FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5. No script edits or copies planned. Memory mirror unavailable because the host has no registered session identity.
L3. Completed with explicit SQL debt preserved: 87 unit +144 integration =231 pass, zero failed/skipped. ValidateReports returns 51910/code1 after rollback: 6 requirements pass,9 fail. New clean/existing test databases retained; no database deleted. API probe8081 stopped; main DB untouched. Hashes unchanged and embedded bytes checked. Requirements original SQL correctness remains partial by explicit user override. No T10, commit or push. Risk: none for the read-only integration; known SQL reporting accuracy risk is documented.
