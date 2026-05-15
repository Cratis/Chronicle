# Aspire Integration

Chronicle provides first-class support for [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview), making it straightforward to run Chronicle as part of an Aspire distributed application. The `Cratis.Chronicle.Aspire` package adds a Chronicle resource to your Aspire AppHost, handling container lifecycle, endpoint wiring, and MongoDB configuration automatically.

## Installation

Add the `Cratis.Chronicle.Aspire` package to your **AppHost** project:

```shell
dotnet add package Cratis.Chronicle.Aspire
```

## Development Mode

For local development, call `AddCratisChronicle()` without arguments. This uses the Chronicle development image (`cratis/chronicle:latest-development`), which bundles MongoDB, so no external database is required.

```csharp
var chronicle = builder.AddCratisChronicle();
```

The development image is ideal for:

- Getting started quickly without additional infrastructure
- Running in CI pipelines where a full stack is needed

## Production Mode

For production or staging environments, use the configure callback. This switches to the production image (`cratis/chronicle:latest`) and lets you wire up an external database resource.

### MongoDB

```csharp
var mongo = builder.AddConnectionString("chronicle-mongo");

var chronicle = builder.AddCratisChronicle(chronicle =>
    chronicle.WithMongoDB(mongo));
```

The `WithMongoDB` method sets the `Cratis__Chronicle__Storage__Type` and `Cratis__Chronicle__Storage__ConnectionDetails` environment variables on the Chronicle container using the connection string from the provided MongoDB resource.

`mongo` can be any `IResourceBuilder<IResourceWithConnectionString>`, including:

- A MongoDB Atlas connection string (`builder.AddConnectionString("...")`)
- A Mongo container added directly in the AppHost: `builder.AddMongoDB("mongo")`
- Any self-hosted or cloud MongoDB resource

### PostgreSQL

```csharp
var postgres = builder.AddConnectionString("chronicle-postgres");

var chronicle = builder.AddCratisChronicle(chronicle =>
    chronicle.WithPostgreSql(postgres));
```

`postgres` can be any `IResourceBuilder<IResourceWithConnectionString>`, including:

- A connection string (`builder.AddConnectionString("...")`)
- A PostgreSQL container: `builder.AddPostgres("postgres").AddDatabase("chronicle")`

### Microsoft SQL Server

```csharp
var sql = builder.AddConnectionString("chronicle-sql");

var chronicle = builder.AddCratisChronicle(chronicle =>
    chronicle.WithMsSql(sql));
```

`sql` can be any `IResourceBuilder<IResourceWithConnectionString>`, including:

- A connection string (`builder.AddConnectionString("...")`)
- A SQL Server container: `builder.AddSqlServer("sql").AddDatabase("chronicle")`

### SQLite

For SQLite, provide the connection string directly (SQLite is file-based and does not require a network resource):

```csharp
var chronicle = builder.AddCratisChronicle(chronicle =>
    chronicle.WithSqlite("Data Source=/data/chronicle.db"));
```

## Connecting a .NET Client

Pass the Chronicle resource as a connection string reference to your application projects so they receive the correct endpoint at runtime:

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithReference(chronicle);
```

The connection string exposed by `ChronicleResource` uses the `chronicle://` scheme and points to the gRPC port (35000 by default), matching the format expected by `ChronicleOptions.FromConnectionString()`.

## Ports

Chronicle exposes the following ports:

| Port  | Endpoint Name | Description                                |
|-------|---------------|--------------------------------------------|
| 35000 | `grpc`        | Primary Chronicle gRPC service             |
| 8080  | `management`  | Management API, Workbench, and health checks |

Both endpoints are registered automatically when you call `AddCratisChronicle()`.

## Complete Example

A typical Aspire AppHost `Program.cs` for a production setup with MongoDB:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddConnectionString("chronicle-mongo");

var chronicle = builder.AddCratisChronicle(chronicle =>
    chronicle.WithMongoDB(mongo));

builder.AddProject<Projects.MyApi>("api")
    .WithReference(chronicle);

builder.Build().Run();
```

For development, simplify further by dropping the MongoDB wiring:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var chronicle = builder.AddCratisChronicle();

builder.AddProject<Projects.MyApi>("api")
    .WithReference(chronicle);

builder.Build().Run();
```

## Docker Images

| Image tag                       | Description                                             |
|---------------------------------|---------------------------------------------------------|
| `cratis/chronicle:latest`       | Production image — no embedded MongoDB                  |
| `cratis/chronicle:latest-development` | Development image — includes embedded MongoDB |
| `cratis/chronicle:latest-development-slim` | Development slim image — no embedded MongoDB |

See [Production Hosting](./production.md) for guidance on running Chronicle in production environments.
