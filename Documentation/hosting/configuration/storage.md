# Storage

Configure the storage backend for Chronicle Server.

## Storage types

| Type | Description |
| --- | --- |
| `MongoDB` | MongoDB (default) |
| `PostgreSql` | PostgreSQL |
| `MsSql` | Microsoft SQL Server |
| `Sqlite` | SQLite (embedded, file-based) |

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

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| type | string | Yes | Storage type: `MongoDB`, `PostgreSql`, `MsSql`, or `Sqlite` |
| connectionDetails | string | Yes | Connection string for the chosen storage backend |

