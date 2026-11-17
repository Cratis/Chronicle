<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/references/sinks.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Configure named sinks in the Kernel

## Contents

- Defaults without a `sinks` section
- One MongoDB and one SQL sink
- Read models in another engine than the events
- Graphs in their own Neo4j servers
- Environment variables
- Startup validation and moves

Requires Chronicle 19.144.0 for the `sinks` section and for clients that name sinks (check the
Kernel image tag and the `Cratis.Chronicle` package version).

## Defaults without a `sinks` section

Without a `sinks` section the Kernel stores read models through its `storage` connection, with the
sink type the client chose (`DefaultSinkTypeId`, MongoDB unless changed), and graphs in its `neo4j`
server. Nothing changes for an existing deployment until the section is added.

The section has three members:

| Member | Default | Meaning |
| --- | --- | --- |
| `readModels` | empty | The configuration a read model naming no sink stores to. Empty keeps `storage` |
| `graphs` | empty | The configuration a graph naming no sink stores to. Empty keeps `neo4j` |
| `configurations.<name>` | empty | One named sink: `type` (`MongoDB`, `SQL` or `Neo4j`) and its own connection. Names match without regard to case |

`MongoDB` and `SQL` take `connectionDetails`; for `SQL` the engine (PostgreSQL, Microsoft SQL
Server or SQLite) is read from the connection string. `Neo4j` takes `server` (a bolt URI),
`userName`, `passwordSecret` (the **name** of the configuration key holding the password, never the
password), `databaseMode` (`PerNamespace` or `Single`), `databasePrefix`, `singleDatabase` and
`createIfMissing`. A named MongoDB or SQL sink derives its database names the way `storage` does,
including `storage.databaseNamePrefix`.

## One MongoDB and one SQL sink

The default read-model sink becomes the `primary` MongoDB configuration, and read models that the
client marks `[Sink("reporting")]` go to PostgreSQL:

```json
{
  "sinks": {
    "readModels": "primary",
    "configurations": {
      "primary": { "type": "MongoDB", "connectionDetails": "<mongodb-connection-string>" },
      "reporting": { "type": "SQL", "connectionDetails": "Host=pg;Database=reporting;Username=chronicle" }
    }
  }
}
```

In these examples `<mongodb-connection-string>` stands for the MongoDB connection string for host `mongo`, port `27017`; write it in your own configuration.

Keep credentials out of the file: supply them through approved configuration, or let
[storage resolution](storage-resolution.md) resolve the connection. A named SQL or Neo4j sink
resolves its connection through `storage.connectionResolution` when that is configured; a named
MongoDB sink uses its `connectionDetails` directly.

## Read models in another engine than the events

Named sinks do not depend on event storage. A Kernel on PostgreSQL can keep read models in
MongoDB, and a Kernel on MongoDB can keep some in PostgreSQL:

```json
{
  "storage": { "type": "postgresql", "connectionDetails": "Host=pg;Database=chronicle;Username=chronicle" },
  "sinks": {
    "readModels": "documents",
    "configurations": {
      "documents": { "type": "MongoDB", "connectionDetails": "<mongodb-connection-string>" }
    }
  }
}
```

The configuration's `type` decides the engine, so a read model naming a `SQL` sink is stored in
SQL even when the client's `DefaultSinkTypeId` is MongoDB.

## Graphs in their own Neo4j servers

```json
{
  "sinks": {
    "graphs": "people",
    "configurations": {
      "people": { "type": "Neo4j", "server": "bolt://people-graph:7687", "userName": "chronicle", "passwordSecret": "NEO4J_PASSWORD" },
      "organization": { "type": "Neo4j", "server": "bolt://organization-graph:7687" }
    }
  }
}
```

## Environment variables

The same keys under the `Cratis__Chronicle__` prefix:

```bash
Cratis__Chronicle__Sinks__ReadModels=primary
Cratis__Chronicle__Sinks__Configurations__primary__Type=MongoDB
Cratis__Chronicle__Sinks__Configurations__primary__ConnectionDetails='<mongodb-connection-string>'
Cratis__Chronicle__Sinks__Configurations__reporting__Type=SQL
Cratis__Chronicle__Sinks__Configurations__reporting__ConnectionDetails=Host=pg;Database=reporting;Username=chronicle
```

## Startup validation and moves

The Kernel refuses to start, naming the configuration, when a `type` is not `MongoDB`, `SQL` or
`Neo4j`; a `MongoDB` or `SQL` entry has no `connectionDetails` or a `Neo4j` entry no `server`;
`readModels` or `graphs` names a missing configuration; or `readModels` names a `Neo4j` entry or
`graphs` a `MongoDB`/`SQL` one. A client registering a read model whose `[Sink]` names no
configuration is refused with `UnknownSinkName`.

Changing `readModels` or `graphs` moves every read model or graph that names no sink. When their
clients register again, the Kernel replays them into the new sink under the
`observers.definitionEvolution` policy (`Manual` records a replay recommendation instead). Plan
the replay time and the new database's capacity before changing it in production: a move is a
replay from the event log, not a data copy.
