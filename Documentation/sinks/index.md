---
uid: Chronicle.Sinks
---
# Sinks

A sink is the storage backend where Chronicle writes read model instances produced by projections and reducers. Chronicle ships with two built-in sink types.

## MongoDB Sink

The default sink. Read model instances are stored as MongoDB documents, one document per instance. The container name defaults to the read model type name (camelCase), controlled by the `INamingPolicy` in use.

This requires no additional setup — MongoDB is the default when you configure Chronicle.

## SQL Sink

The SQL sink treats the database as a document store. Each read model type gets its own table with two columns:

| Column | Type | Description |
|---|---|---|
| `Id` | `nvarchar` / `varchar` | The read model key |
| `Document` | `nvarchar(max)` / `text` | The serialized JSON of the instance |

Tables are created automatically on first use via `IReadModelMigrator`. You do not need to write migrations by hand.

This allows teams using SQL databases (SQL Server, PostgreSQL, SQLite) to take advantage of Chronicle projections without maintaining a MongoDB instance.

### Enabling the SQL Sink

Set `DefaultSinkTypeId` in `ChronicleOptions`:

```csharp
builder.AddCratisChronicle(configureOptions: options =>
{
    options.DefaultSinkTypeId = WellKnownSinkTypes.SQL;
});
```

Or in `appsettings.json`:

```json
{
  "Cratis": {
    "Chronicle": {
      "DefaultSinkTypeId": "f7d3a1e2-4b5c-4d6e-8f9a-0b1c2d3e4f5a"
    }
  }
}
```

### Prerequisites

The SQL sink is provided by the `Cratis.Chronicle.Storage.Sql` NuGet package. Add it to your Kernel host project and register it in the `IChronicleBuilder`:

```csharp
builder.AddCratisChronicleKernel(chronicle =>
{
    chronicle.WithSqlStorage(connectionString);
});
```

Refer to the hosting documentation for full server setup details.

## Choosing a Sink

| | MongoDB | SQL |
|---|---|---|
| Setup complexity | Requires MongoDB | Requires SQL database |
| Schema management | Schemaless | Tables auto-created on first use |
| Query flexibility | Rich aggregation pipeline | Standard SQL queries |
| Best for | Event-driven microservices, flexible schemas | Existing SQL infrastructure, relational tooling |

The sink type applies globally to all projections and reducers registered by the client. It cannot be set per-read-model from client configuration today — all read models share the same sink backend.
