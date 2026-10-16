# Storage Connection Resolution

A multi-tenant platform often decides where each tenant's database lives: which server, which database, which schema, and which identity is allowed in. With a single `connectionDetails`, Chronicle cannot follow that. It derives a database name for every event store and namespace, connects with one credential, and creates whatever is missing.

Connection resolution lets Chronicle ask instead. Whenever it connects to a database, it describes what it is connecting for: the kind of database, the event store, the namespace, and the purpose. A resolver answers with the parts it decides. Anything it leaves out keeps Chronicle's default.

Nothing changes until you configure it. Without a `connectionResolution` section, `connectionDetails` is used exactly as before.

## What Chronicle asks

| Field | Values |
| --- | --- |
| `storage` | `SqlServer`, `PostgreSql`, `Sqlite` or `MongoDB` |
| `purpose` | `Runtime` for reading and writing data, `Schema` for creating databases and schemas, migrating and renaming tables |
| `scope.kind` | `cluster`, `eventStore`, `namespace` (its events) or `readModels` (its projected read models) |
| `scope.eventStore` | The event store, empty for the cluster |
| `scope.namespace` | The namespace, empty for the cluster and an event store |

## What a resolver answers

Every part is optional:

| Part | Effect |
| --- | --- |
| `connectionString` | Replaces `connectionDetails` for this database |
| `database` | The database to use. Chronicle does not derive a name for it, and does not record a [binding](storage.mdx#permanent-bindings) |
| `schema` | The schema Chronicle keeps its tables in (SQL only), overriding `storage.schema` |
| `identity` | `userName` and `password`, or `accessToken`. The token is used as an Entra ID token on SQL Server, as the password on PostgreSQL, and as an OIDC token on MongoDB |
| `expiresAt` | When to ask again |

A connection string or a database means the platform owns the placement of that database. With only an identity, Chronicle keeps deriving and binding names, and only changes who connects. The cluster database is always `connectionDetails`, so for it only the identity and schema are used.

## Two slots and the resolvers

Resolution has two slots with the same shape: `connection` decides where to connect, and `identity` decides who connects. Chronicle applies `connectionDetails`, then what `connection` resolved, then what `identity` resolved. A later part wins. This is Arc's model, so [two slots, one shape](/arc/backend/csharp/tenancy/connection-resolution/#two-slots-one-shape) and [choosing a resolver](/arc/backend/csharp/tenancy/connection-resolution/#choose-a-resolver) apply here unchanged, including the `Default`, `Template`, `Http` and `Custom` resolvers. Chronicle's template placeholders are `{kind}`, `{eventStore}`, `{namespace}`, `{purpose}` and `{storage}`.

The standard `cratis/chronicle` image supports `Template` and `Http`, because both are configuration only. `Custom` needs you to host the kernel in your own process and register an `IConnectionResolver` in code.

## The HTTP contract

The `Http` resolver calls a plain REST endpoint, which you can implement in any language. The contract is the same for MongoDB and SQL storage, and the same one Arc uses: the [connection resolution contract](/arc/connection-resolution-contract/) specifies the request, the response, the status codes, caching and security, and includes an OpenAPI description. Chronicle resolves all of its storage through one resolver, so one endpoint serves every database, whichever storage type you run.

What Chronicle adds is its `scope`, listed under [what Chronicle asks](#what-chronicle-asks). A request for a namespace looks like this:

```http
POST https://platform-mgmt.example/internal/connections
Content-Type: application/json; charset=utf-8

{ "storage": "SqlServer", "purpose": "Schema", "scope": { "kind": "namespace", "eventStore": "orders", "namespace": "acme" } }
```

Your endpoint answers with the parts it decides:

```json
{
  "connectionString": "Server=sql-07.example;Database=tenant-acme;Authentication=Active Directory Managed Identity;User Id=<schema-identity-client-id>;Encrypt=true",
  "schema": "chronicle",
  "expiresAt": "2026-10-15T12:00:00Z"
}
```

Answer `204 No Content` to keep Chronicle's default for a database. Any status other than `200` or `204` fails the connection. Static headers, such as an API key, go under `http.headers`. The default timeout is 10 seconds (`http.timeout`).

## A database per tenant on Azure SQL

This setup has each namespace in a database your platform provisioned, with Entra managed identities for runtime and schema access:

```json
{
  "storage": {
    "type": "MsSql",
    "connectionDetails": "Server=cluster.example;Database=chronicle;Authentication=Active Directory Managed Identity;User Id=<runtime-identity-client-id>;Encrypt=true",
    "schema": "chronicle",
    "createIfMissing": false,
    "connectionResolution": {
      "connection": {
        "resolver": "Http",
        "http": {
          "endpoint": "https://platform-mgmt.example/internal/connections",
          "headers": { "X-Api-Key": "from-your-secret-store" }
        }
      }
    }
  }
}
```

- `connectionDetails` still points at Chronicle's own cluster database.
- Your endpoint answers each `eventStore`, `namespace` and `readModels` request with that tenant's connection string. It uses one managed identity for `Runtime` and another for `Schema`.
- `createIfMissing: false` stops Chronicle from creating databases and schemas. A missing one fails with a clear error instead.
- `schema` keeps Chronicle's tables in the schema you granted. Events and read models can share it.

The schema identity needs to create tables and alter them in that schema. The runtime identity only reads and writes data.

The same configuration through environment variables:

```bash
Cratis__Chronicle__Storage__Schema=chronicle
Cratis__Chronicle__Storage__CreateIfMissing=false
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Resolver=Http
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Endpoint=https://platform-mgmt.example/internal/connections
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Headers__X-Api-Key=from-your-secret-store
```

## Place tenants by convention

When placement follows a naming rule, you do not need an endpoint:

```json
{
  "storage": {
    "type": "PostgreSql",
    "connectionDetails": "Host=pg.example;Database=chronicle;Username=chronicle;Password=secret",
    "connectionResolution": {
      "connection": {
        "resolver": "Template",
        "template": { "database": "{eventStore}_{namespace}_{kind}" }
      }
    }
  }
}
```

The cluster database is always `connectionDetails`: Chronicle resolves only its identity and schema, never its placement. Use `{kind}` when a namespace's events and its read models must not share a database.

## Schema

`storage.schema`, or a resolved `schema`, keeps Chronicle's tables in that schema rather than in the user's default:

- **PostgreSQL**: Chronicle puts the schema first on the connection's search path. It creates the schema when `createIfMissing` allows.
- **SQL Server**: SQL Server places unqualified tables in the default schema of the database user. Chronicle creates the schema when `createIfMissing` allows, then verifies that the user Chronicle migrates as has it as its default schema. If not, it fails with `DefaultSchemaMismatch`. Set the default schema with `ALTER USER [user] WITH DEFAULT_SCHEMA = [chronicle]` for every user Chronicle connects as.
- **SQLite and MongoDB** have no schemas and ignore it.

## Credentials and caching

Caching, expiry and credential handling follow [Arc's rules](/arc/backend/csharp/tenancy/connection-resolution/#caching-and-credentials), with the cache keyed by kind, event store, namespace and purpose. Chronicle adds the following:

- On SQL Server and PostgreSQL, the identity is applied each time a connection opens. A rotated password or refreshed token takes effect without restarting. When a login fails, Chronicle drops the cached resolution and retries through its retry policy, resolving the identity again.
- On MongoDB, an access token is refreshed through the driver's OIDC callback. A resolved password is kept by the database client until the kernel restarts, so prefer tokens there.

## Limits

- The health checks and, on MongoDB, Orleans membership, reminders and the jobs storage connect with `connectionDetails`. Give it an identity that works on its own, for example passwordless Entra authentication in the connection string.
- On SQL, the jobs storage of a namespace migrates its own tables with the `Runtime` resolution.
- Changing a resolution for an existing database moves Chronicle to the new location. It does not move data.
