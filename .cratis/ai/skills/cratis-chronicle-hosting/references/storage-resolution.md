<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/references/storage-resolution.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Resolve kernel storage destinations and identities

Requires Chronicle 19.74.0 for storage connection resolution.
Requires Chronicle 19.74.2 for the kernel startup fix; the first two patches
of this feature fail startup. Check the deployed image tag before changing configuration. Without resolution, configured storage
behavior stays unchanged.

## Scope and purpose

`storage.connectionResolution` uses Arc's connection contract with `Connection`
then `Identity` slots. Choose `Default`, `Template`, `Http`, or `Custom` for an
in-process kernel. The HTTP request scope includes `kind`, `eventStore` and
`namespace`. A resolver may supply connection string, database, schema or
identity independently. Supply credentials through approved configuration or
the trusted resolver, never source files or logs.

Resolution covers event-store, namespace and read-model databases on SQL and
MongoDB. A resolved connection string or database is used as given, without
name derivation or a `DatabaseNames` binding. Moving the resolved destination
is not a data migration. Existing data, schema and permissions must be prepared
at the intended location before traffic is moved.

The cluster database's location stays on `storage.connectionDetails`; its
identity can resolve for `Schema` work. Schema creation, migrations, table
migrations and replay swaps use `Schema`; data access uses `Runtime`. Do not
assume namespace job storage and cluster storage use the same purpose or scope.

## Schema and provisioning

`storage.createIfMissing = false` makes a missing database or schema fail with
`DatabaseDoesNotExist` or `SchemaDoesNotExist` instead of creating it.
`storage.schema` selects Chronicle's SQL schema. PostgreSQL puts it first on
the search path; SQL Server requires the database user's default schema to
match or migration fails with `DefaultSchemaMismatch`. Schema discovery only
considers that schema, not a same-named table elsewhere in a shared database.

## Identity rotation

SQL resolves identities each time a connection opens, including refreshed
passwords or tokens. A failed login drops the cached resolution before retry.
MongoDB separates clients with different credentials and supports MONGODB-OIDC
access tokens refreshed through resolution. Password-based MongoDB clients do
not acquire a new password merely because configuration changes; restart them
when rotating that credential.

## Azure identity

Verified through **v19.110.1**: `Documentation/hosting/azure.md`,
`Documentation/hosting/configuration/storage-connection-resolution.md`,
`Source/Kernel/Core/Configuration/PostgreSqlEntra.cs` and
`Source/Kernel/Storage.Sql/Connections/PostgreSqlEntraAuthentication.cs`.

Requires Chronicle 19.96.0 for AzureCredential HTTP resolver authentication.

The HTTP resolver's `Authentication.Type=AzureCredential` supports
`Credential=ManagedIdentity` (default), `WorkloadIdentity` or `Default`, plus
`Scope` and optional `ClientId`. The resolver identity is not the database
identity. Prefer the specific production credential rather than accidentally
using a developer credential through the default chain.

Requires Chronicle 19.92.2 for native Azure SQL identity authentication.
Requires Chronicle 19.109.0 for native PostgreSQL Entra authentication.

Azure SQL uses Microsoft.Data.SqlClient's native authentication connection-string
settings (managed identity, workload identity, or default chain), not a
Chronicle-specific password flow. Native Entra authentication for
Azure Database for PostgreSQL uses `storage.postgreSqlEntra.enabled=true`, optional
`managedIdentityClientId`, and a PostgreSQL `Username` with **no password**.
Tokens refresh on pooled connections. Do not combine it with a resolver that
returns credentials; placement and schema resolution remain supported.

For separate least-privilege schema/runtime deployments, read
[clustering and schema ownership](clustering-and-schema.md): `--migrate-only`
and `SchemaMode=Verify` are distinct from `createIfMissing=false`.

## Verify

Use the real target provider in integration specs: resolve two namespace
destinations, verify data isolation, run schema-purpose migration with its own
identity, and assert missing schema/database failure when creation is disabled.
Exercise expiry and failed-login invalidation without printing the resolved
credentials. A healthy process alone does not prove the selected destination.
