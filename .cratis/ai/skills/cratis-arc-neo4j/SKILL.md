---
name: cratis-arc-neo4j
description: Use when an Arc C# query or command must read a Neo4j graph, inject the tenant-aware IGraphSession, run Cypher, or choose the graph database per tenant. Not for projecting events onto a graph or for MongoDB documents.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-neo4j/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Read a Neo4j graph from Arc

`Cratis.Arc.Neo4j` gives a model-bound query, a command's `Provide()` method or any
scoped service a session on the Neo4j database of the current tenant. It reads the
graph; it does not build it. A graph is what events leave behind: a graph projection
(`cratis-chronicle-projection`, or `cratis-screenplay-projections` in a model) writes
nodes and relationships through Chronicle's Neo4j sink, and nothing writes to the
graph from a query. Arc does not need Chronicle or event sourcing to read a graph.

Requires `Cratis.Arc.Neo4j` 22.168.0 (`dotnet list package`); older Arc releases have
no graph session, so read the graph with the Neo4j driver directly.

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"): change the model first
  with `cratis-screenplay-event-modeling`. The graph shape comes from the model's
  `graph` declaration; this skill covers only the read side in code.

## When a graph read fits

Use a graph query for traversals: who belongs to which group, nested groups,
paths, "everyone who reaches this node". Use a document read model
(`cratis-chronicle-read-model`, MongoDB or EF Core) for lookups by key, lists and
anything shaped like one row per entity. Do not read a graph for what one document
read already answers.

## Set it up

```bash
dotnet add package Cratis.Arc.Neo4j
```

```json
{
  "Cratis": {
    "Neo4j": {
      "Server": "bolt://localhost:7687",
      "EventStore": "Organization",
      "UserName": "neo4j",
      "PasswordSecret": "NEO4J_PASSWORD"
    }
  }
}
```

`Server` and `EventStore` are required. `EventStore` is the event store whose graph
Chronicle's sink writes; it is the first part of the database name. `PasswordSecret`
names a configuration key (environment variable, user secrets, key vault), so the
password is never written in options. `Password` exists for local development only
and loses to `PasswordSecret`. Other options: `DatabasePrefix`, `DatabaseMode`,
`SingleDatabase`, `ConnectionResolution`.

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j());
```

Use `WithNeo4j()` on the Arc builder, or `AddCratisNeo4j()` on the service
collection for existing host code, never both. Starting the host does not connect;
the first query checks connectivity and permissions.

## Query

```csharp
[ReadModel]
public record GroupMember(PersonId PersonId, PersonName Name)
{
    public static Task<IReadOnlyList<GroupMember>> MembersOf(GroupId groupId, IGraphSession graph) =>
        graph.Query<GroupMember>(
            "MATCH (p:Person)-[:MEMBER_OF]->(g:Group { groupId: $groupId }) RETURN p.personId AS personId, p.name AS name ORDER BY p.name",
            new { groupId });
}
```

- `Query<T>` maps every row; `QuerySingle<T>` maps the first row or gives `null`.
- Parameters are an object's public properties or a dictionary.
- One returned column maps to a primitive, concept or list of those. Several
  columns match constructor parameters or settable properties by name, ignoring
  case. `RETURN p` maps a node's properties.
- `Run` returns the driver's `IResultCursor` and `Session` exposes `IAsyncSession`
  for transaction functions. Pass `graph.Parameters(new { ... })` so the converted
  parameters, including `$namespace`, are used.
- Concepts bind as their inner value and are rebuilt from results. A `Guid` is
  bound and read as its string, a `decimal` as `double`, an enum by name. This is
  what the Chronicle sink stores, so a key written from an event is found by the
  same parameter.

## The session is a read session

The scoped `IGraphSession` opens with `AccessMode.Read`; the server refuses a query
that writes. Change the graph by appending events. For data no projection owns, open
a write session with `IGraphSessionFactory.Open(AccessMode.Write)` and dispose it.
`IGraphSession` is scoped: a singleton that captures it keeps the database of an
earlier scope.

## Pick the tenant's database

`IGraphNamespaceResolver` maps the tenant to a namespace like the Chronicle
integration: a named tenant is the namespace of the same name, and `TenantId.NotSet`
and `TenantId.Default` are `Default`. The database is `[prefix-]eventStore[-namespace]`
and must equal what Chronicle's sink writes, so use the shipped algorithm
(`GraphDatabaseName.For(prefix, eventStore, namespace)`) rather than hand-building
names:

1. Compose the name; the `Default` namespace (exact, case-sensitive), `null` or empty adds no segment.
2. Lower-case it and replace every character outside `[a-z0-9-]` with `-`.
3. Prefix `g-` when it does not start with a letter.
4. When step 2 changed the lower-cased name, or the result is over 63 or under 3
   characters, cut to 54 and append `-` and the first 8 lower-case hex of the SHA-256
   of the unsanitized UTF-8 name.

| Event store | Namespace | Database |
| --- | --- | --- |
| `Orders` | `Default` | `orders` |
| `Orders` | `acme` or `Acme` | `orders-acme` |
| `Orders` | `Acme Corp` | `orders-acme-corp-` plus 8 hex of the hash |
| `1Orders` | `Default` | `g-1orders` |

Lower-casing alone adds no hash, so namespaces that differ only by case share a
database. Replace the naming only when you also change what writes the database:
`WithDatabaseNameResolver<T>()` and `WithNamespaceResolver<T>()` on the Neo4j builder.

Read references/connection-resolution-and-single-mode.md when tenants need their
own server, database or credentials, or when the host runs against Neo4j Community
Edition.

## Test

`Cratis.Arc.Neo4j.Testing` replaces the session without a server:

```csharp
var services = new ServiceCollection();
var graph = services.UseGraphSessionSubstitute();
graph.Returns("MATCH (p:Person)", new GroupMember(personId, new PersonName("Ada")));

var members = await GroupMember.MembersOf(groupId, graph);
```

`Executions` lists the Cypher run with its converted parameters; assert on it to
prove a query was bound and filtered. `Run` and `Session` throw on the substitute,
so keep row mapping on `Query` and `QuerySingle`, and cover driver calls with an
integration test against Neo4j.

## Pitfalls

- A query leaves out `_namespace` in `Single` database mode and reads across tenants.
- A resolved `Database` (template or endpoint) bypasses the naming algorithm, so
  Arc reads a database nothing writes to unless the sink resolves the same one.
- A property starting with an underscore is the sink's own; never return it as data.
- A key keeps its type in the graph: `1` and `"1"` are different nodes.
- Reading the graph from a command to decide an invariant is advisory only; a
  graph is eventually consistent with the event log.

## Verify

Run the host with a query against a development graph: it returns the expected
rows, and with `DatabaseMode` `PerNamespace` another tenant's request returns none
of them. A specification using `UseGraphSessionSubstitute` passes, and `Executions`
shows the `$namespace` parameter. `dotnet build` finishes with zero warnings.
