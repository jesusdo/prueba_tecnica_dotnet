# Validación manual de concurrencia T02

Estas instrucciones usan exclusivamente una base aislada. No ejecutar sobre `PropertyMaintenanceDb` productiva o sobre una base compartida.

## Preparación

1. Crear `PropertyMaintenanceDb_T02_Concurrency` con permisos controlados.
2. Ejecutar DbUp apuntando a esa base, o ejecutar las migraciones aprobadas en orden.
3. En SSMS, seleccionar esa base y ejecutar `PrepareLeaseConcurrency.sql` una sola vez.

## Solapamiento en la misma unidad

1. Abrir dos ventanas independientes de SSMS sobre la misma base.
2. Ejecutar `ConcurrentLeaseSameUnitSessionA.sql` en la ventana A.
3. Antes de que transcurran sus 10 segundos, ejecutar `ConcurrentLeaseSameUnitSessionB.sql` en la ventana B.
4. El resultado esperado es que exactamente una sesión confirme y la otra reciba el error 51021: `Lease period overlaps another lease for the same unit.`
5. Ejecutar una consulta de conteo por `UnitId` y rango de fechas: debe existir como máximo una lease del rango 2040.

## Contratos compatibles

Ejecutar simultáneamente `ConcurrentLeaseCompatibleSessionA.sql` y `ConcurrentLeaseCompatibleSessionB.sql`. Ambas sesiones deben confirmar porque sus periodos no se superponen.

## Unidades diferentes

Ejecutar simultáneamente `ConcurrentLeaseDifferentUnitSessionA.sql` y `ConcurrentLeaseDifferentUnitSessionB.sql`. Ambas sesiones deben confirmar porque usan unidades distintas.

## Limpieza

Eliminar los fixtures por sus valores identificables (`T02 Concurrency Fixture`, `CON-001`, `CON-002` y los correos `t02.concurrency.*@example.test`) dentro de la base aislada. No eliminar una base existente sin autorización.

El orden ascendente de adquisición de `sp_getapplock` evita que dos operaciones multirregistro soliciten las mismas unidades en orden inverso. Si el entorno reporta un deadlock, conservar el error y revisar el deadlock graph; no convertirlo silenciosamente en un resultado exitoso.
