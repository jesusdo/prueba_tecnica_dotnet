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

## Inicialización de base de datos

`DatabaseMigrator` se registra como servicio de `Api` y se ejecuta antes de `app.Run()`. Los scripts de `database/migrations` se embeben en el ensamblado con nombres deterministas y DbUp los ordena por nombre. El journal `dbo.SchemaVersions` evita repetir scripts ya aplicados. La estrategia es una transacción por script: un fallo revierte el script actual, conserva el journal de scripts completados y aborta el arranque de la API.

La base de datos se aprovisiona separadamente. En Azure SQL o producción puede crearse previamente con permisos controlados y luego ejecutar la inicialización con una identidad autorizada para DDL.

## Persistencia y contratos

EF Core 10 se usará Database First cuando T01 implemente el esquema. SQL Server es la fuente de verdad. Las entidades de persistencia no se expondrán directamente: los servicios trabajarán con modelos de negocio y los mapeos serán explícitos.

No se incorporan repositorios genéricos, MediatR, CQRS, AutoMapper ni dependencias innecesarias.

## Fuera de T02

No se implementan seed, entidades EF, repositorios, endpoints ni stored procedures de reportes. El trigger de T02 protege exclusivamente la superposición de contratos; las reglas de aplicación restantes siguen pendientes.
