# AGTec.Persistence.EntityFrameworkCore

EF Core + Npgsql conventions for relational aggregates and transactional data in
AGTecFramework. PostgreSQL is the relational baseline.

Use this package when the model benefits from tables, foreign keys, relational queries,
and EF Core migrations. Use Marten deliberately for document-shaped data, projections,
or event sourcing; it is not the default persistence choice for relational aggregates.

## Install

```bash
dotnet add package AGTec.Persistence.EntityFrameworkCore
```

## Register a context

```csharp
using AGTec.Persistence.EntityFrameworkCore;

services.AddAGTecPostgreSqlDbContext<OrdersDbContext>(
    configuration,
    connectionStringName: "orders");
```

The configuration overload requires `ConnectionStrings:orders` and validates its
PostgreSQL syntax while the host is composed. The direct overload is useful when a
connection string is supplied by service discovery:

```csharp
services.AddAGTecPostgreSqlDbContext<OrdersDbContext>(
    "Host=localhost;Database=orders;Username=postgres;Password=postgres");
```

For integrations that own DbContext registration (for example Wolverine's EF Core
transactional outbox), share the provider convention directly:

```csharp
services.AddDbContextWithWolverineIntegration<OrdersDbContext>(
    options => options.UseAGTecPostgreSql(connectionString),
    connectionString);
```

Apply EF Core migrations explicitly in deployment/CI. Do not use `EnsureCreated` for
production databases.
