# T10.2 authorized remediation
Baseline: 67c8bbd, dev. Delivery: working tree only, no commit or push. Runner: Docker dev, net10.0 Release, xUnit and isolated SQL Server.
Route: parallel bounded writers for independent trigger/configuration units; parent owns schema validator and documentation. No external review services.

## Specs
S1. "No modificar, mover, duplicar, reemplazar ni corregir estos procedimientos." "No modificar sus recursos embebidos."
S2. "No modificar la migración previamente aplicada que creó el trigger." "008_FixLeaseOverlapTrigger.sql"
S3. "Aplicar una corrección mínima que permita operaciones multirregistro válidas sin debilitar la protección contra superposiciones."
S4. "Ajustar la validación para distinguir explícitamente" "Once tablas de negocio y catálogos." "Journals legítimos de DbUp." "Journals del seed." "Objetos inesperados."
S5. "rechazar configuraciones incompatibles con Enlist=false." "La aplicación debe fallar de manera descriptiva antes de aceptar peticiones." "No registrar cadenas de conexión ni credenciales."
S6. "Verificar los dos requisitos clasificados como NOT VERIFIED." "No cambiar un requisito a PASS sin evidencia."
S7. "Los nueve incumplimientos conocidos deben permanecer visibles" "No ejecutar commit ni push." "No avanzar a T11."

## Tasks
- R1 S1-S3 delegated trigger writer: done; RED3 valid batches rejected, GREEN19 lease cases; clean/upgrade001–007 to008 journal verified. No commit.
- R2 S5 delegated configuration writer: done; RED startup reached migrator, GREEN12 unit/1startup; published false rejected safely. No commit.
- R3 S4 inline parent: done; RED51001, GREEN3 schema cases and seedOFF coverage; metadata checks unchanged. No commit.
- R4 S1,S6,S7 inline parent: done; M02/M37 remainNOT VERIFIED with reasons; proposals/docs, fullRelease266 pass, SQL4pass/reportknown51910, hashesunchanged. No commit.

## Log
L1. Authorized scope: "Corregir: - Rechazo de UPDATE multirregistro válido por el trigger de contratos. - Rechazo de journals legítimos por ValidateSchema.sql. - Pérdida de protección transaccional cuando la configuración contiene Enlist=false. - Pruebas de regresión relacionadas con estas correcciones."
L2. Git clean on start. Original SP SHA256: Properties E52171268E2BAAD14E4AF4380830499E54861686AB8512D8FBA833F0770AE09F; Technicians FF5271FB224E4A6706CED7DF032AF6B3113FF100B082425A479A96B33765D9F5. Mirror unavailable: host runtime identity not registered; no memory writes.
L3. Parent reproduced F08 before changing validator: existing fully seeded isolated audit database returns51001, sqlcmd1. New isolated database PropertyMaintenanceDb_T04_T102_20261126 provisioned; no existing database dropped.
L4. Full finalRelease99 unit+167 integration=266 pass,0failed/skipped; explicittrue rollback/status3pass. Existingthreeconcurrency pairs retained behavior; new2UPDATE concurrentcasespass with distinctSPIDs and observed blocking. Default/true supported; publishedfalseexit1 beforemigration/HTTP. Four SQLvalidators0; reports1/51910 with6PASS9knownfails. No manualSQL/resources/oldmigrationchanges, no packages, no commit/push/T11. Local independent static verifier found no reproduced blocker; its checked temp snapshot was removed by parent. Risk4(concurrency) and5(startup/migration); residualmanualSQLdebt unchanged. Mirror remains unavailable under host session identity rule.
