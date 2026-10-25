---
description: Let Chronicle ask a resolver where each tenant's database lives, instead of one connectionDetails, for multi-tenant storage placement.
---
# Storage Connection Resolution

A multi-tenant platform often decides where each tenant's database lives: which server, which database, which schema, and which identity is allowed in. With a single `connectionDetails`, Chronicle cannot follow that. It derives a database name for every event store and namespace, connects with one credential, and creates whatever is missing.

Connection resolution lets Chronicle ask instead. Whenever it connects to a database, it describes what it is connecting for: the kind of database, the event store, the namespace, and the purpose. A resolver answers with the parts it decides. Anything it leaves out keeps Chronicle's default.

Nothing changes until you configure it. For how this fits with selecting tenants and namespaces, see [Tenancy](../../tenancy/index.md). Without a `connectionResolution` section, `connectionDetails` is used exactly as before.

## What Chronicle asks

| Field | Values |
| --- | --- |
| `storage` | `SqlServer`, `PostgreSql`, `Sqlite` or `MongoDB` |
| `purpose` | `Runtime` for reading and writing data, `Schema` for creating databases and schemas, migrating and renaming tables, and offline MongoDB index provisioning or verification |
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

### Which tenant a request is for

Chronicle's requests have no `tenant` key, because the kernel has no concept of a tenant. It isolates by namespace, so the namespace is how your endpoint identifies the tenant:

- `scope.namespace` is the namespace name, never the database name Chronicle derives from it.
- With [Arc](/arc/backend/csharp/chronicle/tenancy/), the tenant id is used verbatim as the namespace name by default. No tenant, or the `Default` tenant, maps to the `Default` namespace.
- Without Arc, or with a namespace resolver of your own, the namespace is whatever that [namespace resolver](../../namespaces/aspnetcore.mdx) picks. If it differs from your tenant id, map it back in your endpoint.
- Combine it with `scope.eventStore` when the same tenant exists in more than one event store, and use `scope.kind` to tell its events (`namespace`) from its read models (`readModels`).
- `eventStore` and `cluster` requests are not for any tenant, and their `namespace` is empty.

When Arc resolves the connections for an application's own storage, it sends `scope.tenant` instead. One endpoint can serve both kinds of caller by checking which keys are present. With Arc's default mapping, Arc's `scope.tenant` and Chronicle's `scope.namespace` carry the same value, `Default` included.

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

## Authenticate the resolver

The standard image can authenticate calls to your HTTP resolver with a static bearer token, OAuth client credentials, or Microsoft Entra ID. Configure the `http` object under `storage.connectionResolution.connection` (or `identity` when that slot uses HTTP). This authenticates **Chronicle to the resolver**, independently of how Chronicle authenticates to SQL or MongoDB. Without authentication settings, existing behavior is unchanged.

Supply tokens and client secrets from your deployment's secret store, not checked-in configuration. Use HTTPS for the resolver and token endpoints. See [Arc's resolver authentication](https://www.cratis.io/arc/backend/csharp/tenancy/connection-resolution/#authenticate-to-the-endpoint) for the authentication options.

### Static bearer token

Set the header directly in the `http` object:

```json
{
  "endpoint": "https://platform-mgmt.example/internal/connections",
  "headers": { "Authorization": "Bearer <token-from-your-secret-store>" }
}
```

The equivalent environment variable, alongside the resolver and endpoint settings above:

```bash
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Headers__Authorization='Bearer <token-from-your-secret-store>'
```

### OAuth client credentials

Configure the `http` object to obtain and refresh bearer tokens:

```json
{
  "endpoint": "https://platform-mgmt.example/internal/connections",
  "authentication": {
    "type": "ClientCredentials",
    "tokenEndpoint": "https://login.example/oauth2/token",
    "clientId": "chronicle",
    "clientSecret": "<from-your-secret-store>",
    "scope": "connections"
  }
}
```

```bash
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Type=ClientCredentials
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__TokenEndpoint=https://login.example/oauth2/token
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__ClientId=chronicle
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__ClientSecret='<from-your-secret-store>'
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Scope=connections
```

### Microsoft Entra ID

For a user-assigned managed identity, configure the `http` object with its client id and the resolver API's scope:

```json
{
  "endpoint": "https://platform-mgmt.example/internal/connections",
  "authentication": {
    "type": "AzureCredential",
    "credential": "ManagedIdentity",
    "clientId": "<resolver-identity-client-id>",
    "scope": "api://platform-mgmt/.default"
  }
}
```

```bash
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Type=AzureCredential
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Credential=ManagedIdentity
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__ClientId='<resolver-identity-client-id>'
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Scope=api://platform-mgmt/.default
```

`ManagedIdentity` is the default credential; omit `clientId` for a system-assigned identity. Set `credential` to `WorkloadIdentity` for Kubernetes workload identity, or `Default` for the Azure SDK's default credential chain (including developer credentials locally). `clientId` selects the user-assigned managed identity or workload identity application, not the SQL database identity.

For workload identity, change `"credential": "WorkloadIdentity"` in the JSON above, or set:

```bash
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Credential=WorkloadIdentity
AZURE_CLIENT_ID='<workload-identity-application-client-id>'
AZURE_TENANT_ID='<entra-tenant-id>'
AZURE_FEDERATED_TOKEN_FILE=/var/run/secrets/azure/tokens/azure-identity-token
```

Provide the Azure SDK environment variables and mounted federated token through your workload identity setup. Set the resolver's `clientId` to the workload identity application's client id.

Do not combine `authentication.type` other than `None` with an `Authorization` header: both try to supply the bearer token and the resolver rejects that configuration. Tokens are cached and refreshed before expiry; the default `authentication.refreshMargin` is five minutes.

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

## Microsoft Entra ID authentication

On SQL Server, the kernel supports every Microsoft Entra ID mode of the `Authentication` keyword in a connection string, whether it is in `connectionDetails` or returned by a resolver:

| Mode | Notes |
| --- | --- |
| `Active Directory Managed Identity` | A system-assigned identity needs no `User Id`. For a user-assigned identity, set `User Id=<client-id>` |
| `Active Directory Default` | Tries the environment, workload identity, managed identity and developer tools in turn |
| `Active Directory Workload Identity` | For Kubernetes (AKS) pods. Provide the `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_FEDERATED_TOKEN_FILE` environment variables, which the workload identity webhook injects |
| `Active Directory Service Principal` | `User Id=<client-id>` and `Password=<client-secret>` |

Do not combine an `identity.accessToken` returned by a resolver with `Authentication=`, `Password` or `Integrated Security` in `connectionDetails` or in the resolved `connectionString`. SqlClient rejects a connection that has both a token and a credential. Pick one way per connection: either the connection string carries the authentication mode, or the resolver returns the token on an otherwise credential-free connection string.

For Azure Database for PostgreSQL, [native Entra authentication](../azure.md#azure-database-for-postgresql) acquires and refreshes tokens without a resolver. Enable `storage.postgreSqlEntra.enabled`, optionally set `storage.postgreSqlEntra.managedIdentityClientId`, and provide the PostgreSQL `Username` but no password in the connection string. Do not combine native authentication with a resolver returning credentials; placement and schema resolution remain supported.

With native authentication disabled (the default), a resolver can instead return the Entra token as `identity.accessToken`, and Chronicle uses it as the password:

```json
{
  "identity": { "userName": "chronicle-runtime@example.com", "accessToken": "<entra-access-token>" },
  "expiresAt": "2026-10-15T12:00:00Z"
}
```

Request the token for the `https://ossrdbms-aad.database.windows.net/.default` scope and set `expiresAt` before it expires, so Chronicle asks again.

## Credentials and caching

Caching, expiry and credential handling follow [Arc's rules](/arc/backend/csharp/tenancy/connection-resolution/#caching-and-credentials), with the cache keyed by kind, event store, namespace and purpose. Chronicle adds the following:

- On SQL Server and PostgreSQL, the identity is applied each time a connection opens. A rotated password or refreshed token takes effect without restarting. When a login fails, Chronicle drops the cached resolution and retries through its retry policy, resolving the identity again.
- On MongoDB, an access token is refreshed through the driver's OIDC callback. A resolved password is kept by the database client until the kernel restarts, so prefer tokens there.

## Limits

For a task-oriented walkthrough, see [Run Chronicle on Azure](../azure.md).

- On SQL Server and PostgreSQL, the health check connects to the cluster database through the same resolution as the storage, with the identity resolved for the cluster.
- On MongoDB, the health check, Orleans membership, reminders and the jobs storage connect with `connectionDetails`. Give it an identity that works on its own.
- On SQL, the jobs storage of a namespace migrates its own tables with the `Runtime` resolution.
- Changing a resolution for an existing database moves Chronicle to the new location. It does not move data.
