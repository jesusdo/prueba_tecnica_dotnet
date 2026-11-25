# Arquitectura aprobada

## Decisión

La solución usa un monolito ASP.NET Core en capas pragmáticas dentro de `PropertyMaintenance.Api`, acompañado por dos proyectos de pruebas. El flujo es:

```text
Controller -> Service -> Repository -> SQL Server
```

La decisión V2 sustituye la separación anterior en proyectos Domain/Application/Infrastructure. El cambio es deliberado: el alcance de la prueba es acotado, tres proyectos reducen coordinación y sobreabstracción, y la estructura interna conserva separación clara entre negocio, persistencia, transporte y mapeos.

## Proyectos

| Proyecto | Responsabilidad | Referencias |
|---|---|---|
| `PropertyMaintenance.Api` | API, servicios, modelos, persistencia, mapeos y excepciones | Ninguna de los proyectos propios |
| `PropertyMaintenance.UnitTests` | Pruebas unitarias de reglas y servicios | `PropertyMaintenance.Api` |
| `PropertyMaintenance.IntegrationTests` | Pruebas de integración de API y SQL Server | `PropertyMaintenance.Api` |

Todos los proyectos usan `net10.0`. No hay dependencias circulares.

## Organización interna de Api

```text
Controllers/
DTOs/
Business/Services/
Business/Models/
Business/Enums/
DataAccess/Entities/
DataAccess/Repositories/
DataAccess/Context/
DataAccess/Migrations/
Mappings/
Exceptions/
```

`DataAccess/Migrations` es el espacio de scripts versionados y soporte DbUp; no se utilizarán migraciones de EF Core.

## Persistencia y contratos

EF Core 10 se usará Database First cuando T01 implemente el esquema. SQL Server es la fuente de verdad. Las entidades de persistencia no se expondrán directamente: los servicios trabajarán con modelos de negocio y los mapeos serán explícitos.

No se incorporan repositorios genéricos, MediatR, CQRS, AutoMapper ni dependencias innecesarias.

## Fuera de T00

No se implementan reglas, endpoints, entidades, tablas, consultas de reportes, seed ni configuración de base de datos en esta tarea.
