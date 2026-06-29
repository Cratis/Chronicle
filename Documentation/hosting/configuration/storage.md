# Storage

Configure the storage backend for Chronicle Server.

## Storage types

| Type | Description |
| --- | --- |
| `MongoDB` | MongoDB (default) |
| `PostgreSql` | PostgreSQL |
| `MsSql` | Microsoft SQL Server |
| `Sqlite` | SQLite (embedded, file-based) |
| `InMemory` | In-memory (ephemeral, non-durable) |

## MongoDB

```json
{
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://localhost:27017"
  }
}
```

## PostgreSQL

```json
{
  "storage": {
    "type": "PostgreSql",
    "connectionDetails": "Host=localhost;Port=5432;Database=chronicle;Username=chronicle;Password=secret"
  }
}
```

## Microsoft SQL Server

```json
{
  "storage": {
    "type": "MsSql",
    "connectionDetails": "Server=localhost;Database=chronicle;User Id=chronicle;Password=secret;TrustServerCertificate=True"
  }
}
```

## SQLite

```json
{
  "storage": {
    "type": "Sqlite",
    "connectionDetails": "Data Source=/data/chronicle.db"
  }
}
```

## In-memory

The in-memory backend keeps every event sequence, observer, read-model sink, and system record in process memory. There is no connection string, and `connectionDetails` is ignored.

```json
{
  "storage": {
    "type": "InMemory"
  }
}
```

> [!CAUTION]
> The in-memory backend is **not durable** — all data is lost when the process exits, and it is scoped to a single process (it does not share state across a cluster). Use it for tests, samples, and ephemeral environments, not for production data.

When you host Chronicle programmatically, select the in-memory backend with the `WithInMemory()` builder extension instead of configuration:

```csharp
siloBuilder.AddChronicleToSilo(chronicle => chronicle.WithInMemory());
```

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| type | string | Yes | Storage type: `MongoDB`, `PostgreSql`, `MsSql`, `Sqlite`, or `InMemory` |
| connectionDetails | string | Conditional | Connection string for the chosen storage backend. Required for all backends except `InMemory`, which ignores it |

