---
title: Run Chronicle on Azure with Microsoft Entra ID
description: Deploy the Chronicle kernel on Azure with Azure SQL or Azure Database for PostgreSQL, managed identities instead of passwords, per-tenant connection resolution, health checks and Entra-protected webhooks.
---

This guide shows a platform engineer how to run the Chronicle kernel on Azure without database passwords: storage on Azure SQL or Azure Database for PostgreSQL, a managed identity for the kernel, per-tenant database placement resolved over HTTP, and webhooks that call Entra-protected endpoints. You finish with one set of environment variables that fits Azure Container Apps or AKS.

Each step links to the reference page that lists every option. This page only shows how they fit together.

## Prerequisites

- The `cratis/chronicle` image. Its SQL Server support includes `Microsoft.Data.SqlClient.Extensions.Azure`, which provides the Entra authentication modes, and its HTTP connection resolver can authenticate with Entra ID.
- A **user-assigned managed identity** for the kernel. On AKS, use [workload identity](https://learn.microsoft.com/azure/aks/workload-identity-overview) bound to an Entra application instead.
- An Azure SQL database or an Azure Database for PostgreSQL server that the identity can reach.

Use one identity for the kernel and grant it only what each step below needs. Its `clientId` is what you put in connection strings and resolver settings. A system-assigned identity also works where a step says so.

## Store events in Azure SQL

Create a database user for the identity with `CREATE USER [chronicle-kernel] FROM EXTERNAL PROVIDER`, and grant it the permissions to read and write data and, because Chronicle creates and migrates its own tables, to create and alter them. To split those permissions, resolve a schema identity and a runtime identity separately by [purpose](configuration/storage-connection-resolution.md#what-chronicle-asks). Then point `connectionDetails` at the database with an Entra mode instead of a password:

```bash
Cratis__Chronicle__Storage__Type=MsSql
Cratis__Chronicle__Storage__ConnectionDetails='Server=myserver.database.windows.net;Database=chronicle;Authentication=Active Directory Managed Identity;User Id=<managed-identity-client-id>;Encrypt=true'
```

The kernel supports every Entra mode of the `Authentication` keyword, so `Active Directory Workload Identity` is the choice on AKS, where the workload identity webhook injects `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_FEDERATED_TOKEN_FILE`. The modes, and what you must not combine with a resolved token, are listed under [Microsoft Entra ID authentication](configuration/storage-connection-resolution.md#microsoft-entra-id-authentication).

## Store events in Azure Database for PostgreSQL

PostgreSQL has no `Authentication=` mode in the connection string. Chronicle does not fetch an Entra token for Npgsql itself. The one supported path is a [connection resolver](configuration/storage-connection-resolution.md) that returns an Entra access token as `identity.accessToken`, which Chronicle then uses as the password:

```json
{
  "identity": { "userName": "chronicle-kernel", "accessToken": "<entra-access-token>" },
  "expiresAt": "2026-10-15T12:00:00Z"
}
```

Your resolver requests the token for the `https://ossrdbms-aad.database.windows.net/.default` scope and sets `expiresAt` before the token expires. Chronicle applies the identity every time a connection opens, so a refreshed token takes effect without a restart.

To use token authentication on PostgreSQL you therefore need a resolver, in the `identity` slot or the `connection` slot. Without one, authenticate with a user name and password in `connectionDetails`. Set the storage type and a connection string without a password:

```bash
Cratis__Chronicle__Storage__Type=PostgreSql
Cratis__Chronicle__Storage__ConnectionDetails='Host=myserver.postgres.database.azure.com;Database=chronicle;Username=chronicle-kernel;SSL Mode=Require'
```

## Resolve tenant databases over HTTP with Entra ID

When each tenant, which Chronicle models as a namespace, lives in a database your platform provisions, have Chronicle ask an HTTP endpoint where to connect. Chronicle sends the kind of database, event store, namespace and purpose. Your endpoint answers with a connection string, a schema, an identity and an expiry. The [HTTP contract](configuration/storage-connection-resolution.md#the-http-contract) specifies the request and response, and [which tenant a request is for](configuration/storage-connection-resolution.md#which-tenant-a-request-is-for) explains how `scope.namespace` maps to your tenant id.

Authenticate Chronicle to your endpoint with the kernel's managed identity, so there is no shared secret:

```bash
Cratis__Chronicle__Storage__CreateIfMissing=false
Cratis__Chronicle__Storage__Schema=chronicle
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Resolver=Http
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Endpoint=https://platform-mgmt.example/internal/connections
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Type=AzureCredential
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Credential=ManagedIdentity
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__ClientId=<managed-identity-client-id>
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Scope=api://platform-mgmt/.default
```

- `Credential` is `ManagedIdentity` by default. Omit `ClientId` for a system-assigned identity, and set `Credential=WorkloadIdentity` on AKS (the Azure SDK variables above must then be present).
- `Scope` is the resolver API's scope. `ClientId` selects the identity that calls the resolver, not the database identity.
- Do not also set an `Authorization` header: both would supply the bearer token, and the resolver rejects that configuration.
- `CreateIfMissing=false` stops Chronicle from creating databases, so a database your platform has not provisioned fails with a clear error.
- With `CreateIfMissing=true` on SQL Server, kernels coordinate database creation through a lock in `master`. An identity that cannot open `master`, such as a contained database user, skips that lock and only checks that its database exists. It cannot create a missing database, so provision those databases up front.

[Authenticate the resolver](configuration/storage-connection-resolution.md#authenticate-the-resolver) also covers static bearer tokens and OAuth client credentials. [A database per tenant on Azure SQL](configuration/storage-connection-resolution.md#a-database-per-tenant-on-azure-sql) shows a complete resolved setup.

## Know what `/health` checks

On SQL Server and PostgreSQL, the kernel registers a database health check, named `mssql` or `postgresql`, behind the `/health` endpoint. It connects to the **cluster** database the way the storage does, through the identity resolved for the cluster at `Runtime` purpose, and runs `SELECT 1`. The check times out after three seconds. A rotated token or a managed identity that lost its access therefore turns `/health` unhealthy, which is what you want a probe to see.

The check covers the cluster database. It does not open a connection to each tenant database. With MongoDB storage, the health check connects with `connectionDetails`.

For probes from Azure load balancers or Container Apps, publish a dedicated plaintext port rather than fighting certificate validation:

```bash
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
```

See [Health endpoint](configuration/health-endpoint.md) for the dedicated port options.

## Call Entra-protected endpoints from webhooks

A webhook the kernel calls can authenticate with OAuth client credentials. In the .NET client, register it with `WithOAuth(authority, clientId, clientSecret, scope)`, as in the [webhook registration example](../webhooks/index.md#register-a-webhook):

| Argument | Entra value |
| --- | --- |
| `authority` | `https://login.microsoftonline.com/<entra-tenant-id>/v2.0` |
| `clientId` | The client id of the app registration that calls the receiver |
| `clientSecret` | Its client secret, read from your secret store |
| `scope` | `api://<receiver-application-id>/.default` |

Chronicle discovers the token endpoint from `<authority>/.well-known/openid-configuration`, requests a token with the `client_credentials` grant and the scope, and refreshes it shortly before expiry. A `401` triggers one refresh and retry. Webhooks use a client secret: unlike the storage and resolver settings above, they do not use the kernel's managed identity. Keep the secret in your secret store. See [OAuth client credentials](../webhooks/index.md#oauth-client-credentials) for the details, and [external services](../clients/dotnet/external-services.md) for the same scope on external service registrations.

## Plan for availability

On Azure SQL and Azure Database for PostgreSQL you can run more than one kernel node by opting in to SQL clustering. It is never on by default: nodes left on the default `Localhost` type do not join one cluster, even when they share a database. Set the type on every node:

```bash
Cratis__Chronicle__Clustering__Type=Sql
Cratis__Chronicle__Clustering__ClusterId=chronicle
Cratis__Chronicle__Clustering__ServiceId=chronicle
```

- `Sql` requires `MsSql` or `PostgreSql` storage. SQLite and in-memory storage do not support multi-node clustering, and an unsupported combination fails at startup.
- Every node must use the same cluster database, schema, `clusterId` and `serviceId`.
- Membership uses the same connection resolution as storage, so the managed identity needs no extra setup.
- The silo (11111) and gateway (30000) ports must be reachable between nodes. Set `advertisedIP` when the default address is not.

Clustering gives you more than one node. It does not by itself make a deployment zero-downtime, and this guide makes no such promise. Test how your rollout behaves before relying on it, and follow the [rolling-deploy runbook](rolling-deploy.md) when you replace nodes. See [Clustering](configuration/clustering.md) for the full set of options, limits and defunct-silo cleanup.

## Put it together

This is a minimal Azure SQL configuration with HTTP resolution and the dedicated health port, as environment variables for Container Apps or AKS:

```bash
# Storage: Azure SQL, authenticated as the managed identity
Cratis__Chronicle__Storage__Type=MsSql
Cratis__Chronicle__Storage__ConnectionDetails='Server=myserver.database.windows.net;Database=chronicle;Authentication=Active Directory Managed Identity;User Id=<managed-identity-client-id>;Encrypt=true'
Cratis__Chronicle__Storage__Schema=chronicle
Cratis__Chronicle__Storage__CreateIfMissing=false

# Per-tenant placement, resolver called as the managed identity
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Resolver=Http
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Endpoint=https://platform-mgmt.example/internal/connections
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Type=AzureCredential
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Credential=ManagedIdentity
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__ClientId=<managed-identity-client-id>
Cratis__Chronicle__Storage__ConnectionResolution__Connection__Http__Authentication__Scope=api://platform-mgmt/.default

# Probes
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
```

If every tenant shares the one database, drop the `ConnectionResolution` lines and `CreateIfMissing=false`, so Chronicle can create the databases it derives. For SQL Server, `Schema` also requires the schema to be the default schema of the user Chronicle connects as, see [Schema](configuration/storage-connection-resolution.md#schema). Start one node, call `/health` on port 8080, and expect a healthy response.

## See also

- [Production deployment](production.md)
- [Storage](configuration/storage.mdx)
- [Storage connection resolution](configuration/storage-connection-resolution.md)
- [Health endpoint](configuration/health-endpoint.md)
- [Clustering](configuration/clustering.md)
- [Roll out a new version to a multi-node cluster](rolling-deploy.md)
