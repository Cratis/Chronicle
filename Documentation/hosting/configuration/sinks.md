---
title: Configure named sinks in Cratis Chronicle Server
description: Configure named sinks so a Cratis Chronicle Kernel stores read models and graphs in several MongoDB, SQL and Neo4j servers, independent of its event storage.
---
The `sinks` section names the sinks a Kernel can store read models and graphs in. Each named sink is a sink type with its own connection, independent of the Kernel's [storage](storage). A read model or a graph chooses one with `[Sink("name")]` in the client, as described in [Named sinks](../../sinks/index.mdx#named-sinks).

Without a `sinks` section the Kernel behaves as it always has: read models are stored through the `storage` connection with the sink type the client chose, and graphs in the `neo4j` server.

## Example configuration

```json
{
  "sinks": {
    "readModels": "primary",
    "graphs": "people",
    "configurations": {
      "primary": { "type": "MongoDB", "connectionDetails": "mongodb://mongo:27017" },
      "reporting": { "type": "SQL", "connectionDetails": "Host=pg;Database=reporting;Username=chronicle" },
      "people": { "type": "Neo4j", "server": "bolt://neo4j:7687", "userName": "chronicle", "passwordSecret": "NEO4J_PASSWORD", "databaseMode": "Single" }
    }
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| readModels | string | empty | The configuration read models store to when they name no sink. Empty keeps the `storage` connection |
| graphs | string | empty | The configuration graphs store to when they name no sink. Empty keeps the `neo4j` server |
| configurations | object | empty | The named sink configurations, by name. Names are matched without regard to case |

`readModels` applies to read models whose sink type is `MongoDB` or `SQL`, and `graphs` to graphs whose sink type is `Neo4j`. Passive read models, event targets and the in-memory sinks keep their own sink.

## Sink configuration properties

| Property | Applies to | Default | Description |
| --- | --- | --- | --- |
| type | all | none | `MongoDB` or `SQL` for read models, `Neo4j` for graphs |
| connectionDetails | MongoDB, SQL | none | The connection string. For `SQL` the engine, PostgreSQL, Microsoft SQL Server or SQLite, is read from it |
| connectionDetailsFile | MongoDB, SQL | none | Path of a file holding the connection string, such as a mounted Kubernetes Secret. Set either this or `connectionDetails`, not both |
| server | Neo4j | none | The bolt URI of the server, such as `bolt://neo4j:7687` |
| serverFile | Neo4j | none | Path of a file holding the bolt URI of the server, such as a mounted Kubernetes Secret. Set either this or `server`, not both |
| userName | Neo4j | none | The user to authenticate with |
| passwordSecret | Neo4j | none | The name of the configuration key that holds the password. The password itself is never written in the options |
| passwordFile | Neo4j | none | Path of a file holding the password, such as a mounted Kubernetes Secret. Set either this or `passwordSecret`, not both |
| databasePrefix | Neo4j | none | Prefix of the database names |
| databaseMode | Neo4j | PerNamespace | `PerNamespace` for a database per event store and namespace, or `Single` for one database |
| singleDatabase | Neo4j | neo4j | The database used in `Single` mode |
| createIfMissing | Neo4j | true | Create a missing database on first use |

A named MongoDB or SQL sink names its databases the way the Kernel's own storage does, including `storage.databaseNamePrefix`. A named Neo4j sink names them as described in [Neo4j graph sink](../../sinks/neo4j.mdx).

## An example per engine

Store read models in MongoDB while the Kernel keeps its events in PostgreSQL:

```json
{
  "storage": { "type": "postgresql", "connectionDetails": "Host=pg;Database=chronicle;Username=chronicle" },
  "sinks": {
    "configurations": {
      "documents": { "type": "MongoDB", "connectionDetails": "mongodb://mongo:27017" }
    }
  }
}
```

Store some read models in PostgreSQL while the Kernel runs on MongoDB:

```json
{
  "storage": { "type": "mongodb", "connectionDetails": "mongodb://mongo:27017" },
  "sinks": {
    "configurations": {
      "reporting": { "type": "SQL", "connectionDetails": "Host=pg;Database=reporting;Username=chronicle" }
    }
  }
}
```

Use Microsoft SQL Server or SQLite the same way, with their connection strings:

```json
{
  "sinks": {
    "configurations": {
      "warehouse": { "type": "SQL", "connectionDetails": "Server=sql;Database=warehouse;User Id=chronicle;TrustServerCertificate=true" },
      "local": { "type": "SQL", "connectionDetails": "Data Source=read-models.db" }
    }
  }
}
```

Serve two graphs from two Neo4j servers in one event store:

```json
{
  "sinks": {
    "graphs": "people",
    "configurations": {
      "people": { "type": "Neo4j", "server": "bolt://people-graph:7687" },
      "organization": { "type": "Neo4j", "server": "bolt://organization-graph:7687" }
    }
  }
}
```

The same configuration through environment variables:

```bash
Cratis__Chronicle__Sinks__Graphs=people
Cratis__Chronicle__Sinks__Configurations__people__Type=Neo4j
Cratis__Chronicle__Sinks__Configurations__people__Server=bolt://people-graph:7687
Cratis__Chronicle__Sinks__Configurations__organization__Type=Neo4j
Cratis__Chronicle__Sinks__Configurations__organization__Server=bolt://organization-graph:7687
```

## Connections from files

A connection string or a password is a Secret, and in Kubernetes the safe way to hand a Secret to a pod is a mounted file rather than an environment variable. Name the file with `connectionDetailsFile` instead of `connectionDetails`, and for Neo4j with `serverFile` instead of `server` and `passwordFile` instead of `passwordSecret`:

```json
{
  "sinks": {
    "configurations": {
      "reporting": { "type": "SQL", "connectionDetailsFile": "/var/run/secrets/sinks/reporting" },
      "people": { "type": "Neo4j", "serverFile": "/var/run/secrets/sinks/people-server", "userName": "chronicle", "passwordFile": "/var/run/secrets/sinks/people-password" }
    }
  }
}
```

The same through environment variables:

```bash
Cratis__Chronicle__Sinks__Configurations__reporting__ConnectionDetailsFile=/var/run/secrets/sinks/reporting
Cratis__Chronicle__Sinks__Configurations__people__ServerFile=/var/run/secrets/sinks/people-server
Cratis__Chronicle__Sinks__Configurations__people__PasswordFile=/var/run/secrets/sinks/people-password
```

Each file is read once, at startup, and trimmed of a trailing newline. A password read from a file is kept in the Kernel's configuration and `passwordSecret` points at it, so it is never written in the options. The Kernel's own storage takes `storage.connectionDetailsFile` the same way; see [Storage](storage#read-the-connection-from-a-file). The default graph connection takes `neo4j.serverFile` and `neo4j.passwordFile`; see [Neo4j graph sink](../../sinks/neo4j.mdx#read-the-connection-from-files).

## Validation at startup

The Kernel validates the section before it starts and refuses to start with a message naming the configuration when:

- a configuration has a type other than `MongoDB`, `SQL` or `Neo4j`;
- a `MongoDB` or `SQL` configuration has no `connectionDetails`, or a `Neo4j` configuration has no `server`;
- `readModels` or `graphs` names a configuration that does not exist;
- `readModels` names a `Neo4j` configuration, or `graphs` names a `MongoDB` or `SQL` configuration.
- a setting and its file sibling are both set, such as `connectionDetails` and `connectionDetailsFile`, `server` and `serverFile`, or `passwordSecret` and `passwordFile`; the message names both;
- a file named by `connectionDetailsFile`, `serverFile` or `passwordFile` does not exist, is a directory, cannot be read, or is empty; the message names the setting and the path, never what the file holds.

A read model that names a sink the Kernel has no configuration for is refused when the client registers it.

Changing `readModels` or `graphs` moves the read models and graphs that name no sink. When their clients register again, the Kernel replays them into the new sink, following the `observers.definitionEvolution` policy.

## Connections

A named SQL or Neo4j sink resolves its connections through [storage connection resolution](storage-connection-resolution.md) when it is configured, with the sink's engine as the requested storage. A named MongoDB sink connects with its `connectionDetails` directly.
