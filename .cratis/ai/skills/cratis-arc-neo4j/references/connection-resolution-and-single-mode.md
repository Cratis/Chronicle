<!-- cratis-ai-managed: skills/cratis-arc-neo4j/references/connection-resolution-and-single-mode.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Connection resolution and single-database mode

## Single-database mode

`DatabaseMode` `Single` puts every namespace in `SingleDatabase` (`neo4j` by
default), the only user database on Neo4j Community Edition. Chronicle stamps the
namespace as `_namespace` on every node and relationship, and nothing else separates
tenants. Use it for development and tests, never production. Filter every pattern
with the `$namespace` parameter the session adds to each query:

```cypher
MATCH (p:Person)-[:MEMBER_OF]->(g:Group)
WHERE p._namespace = $namespace AND g._namespace = $namespace
RETURN p.name AS name, g.name AS group
```

In `PerNamespace` mode the parameter is still given and harmless, so one query can
carry the filter in both modes. `GraphParameters.Namespace` and
`GraphParameters.NamespaceProperty` name the parameter and the property.

## Connection resolution

Resolution lets the platform choose server, database or credentials per tenant.
Arc resolves once per service scope, and the scoped `IGraphSession` and
`IGraphSessionFactory` share it. A resolved part applies as follows:

| Resolved part | Effect |
| --- | --- |
| `ConnectionString` | A Bolt URI that replaces `Server` |
| `Database` | Replaces the computed database name |
| `Identity.UserName` and `Identity.Password` | Basic authentication |
| `Identity.AccessToken` | Bearer token |

With no resolved identity the configured user and password apply. Drivers are shared
per server and resolution, never across credentials. When the server rejects a
credential or token, Arc drops the cached resolution and resolves again.

To resolve only the identity over HTTP and keep the shared server and naming:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j(
    configureOptions: neo4j => neo4j.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"))));
```

A template placing tenants by rule goes in the `ConnectionResolution` section with
`"Resolver": "Template"` and a `Template` holding a `Database` such as
`graph-{tenant}`. The default tenant then resolves to `graph-Default`, so point the
Chronicle sink at the same destination.
