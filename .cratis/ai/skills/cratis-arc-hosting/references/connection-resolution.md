<!-- cratis-ai-managed: skills/cratis-arc-hosting/references/connection-resolution.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Resolve application storage connections

Requires Arc 22.109.0. Check with `dotnet list package`; older Arc has no shared
connection resolver. Tenant resolution alone does not change the database.

## Select the resolution

`ArcOptions.ConnectionResolution`, bound under `Cratis:Arc:ConnectionResolution`,
is the shared default for MongoDB and Entity Framework Core. An integration's
own `ConnectionResolution` replaces that entire default for that integration;
it is not merged field by field.

Resolution has two slots, `Connection` then `Identity`, using the same shape.
Choose `Default` for configured values, `Template` for scope-derived values,
`Http` for a trusted platform endpoint, or `Custom` for `IConnectionResolver`.
`ConnectionRequest`, `ResolvedConnection`, `ConnectionIdentity` and
`ConnectionPurpose` live in `Cratis.Arc.Connections`. A result can supply only
the connection string, database, schema or identity it owns; fields are optional.

Results are cached in memory per request key, honor `expiresAt`, and are
invalidated on login failure. This is credential resolution, not permission to
move data. Changing the resolved destination does not migrate an existing database.
Keep resolver endpoints and templates within the approved tenant/storage boundary;
never log resolved passwords or tokens.

## MongoDB

`MongoDBOptions.ConnectionResolution` can be bound from
`Cratis:MongoDB:ConnectionResolution`. A resolved username/password becomes a
SCRAM credential. An access token becomes a MONGODB-OIDC credential refreshed
through the resolver. Clients on the same server with different credentials
are not shared as one identity.

## Entity Framework Core

`EntityFrameworkCoreOptions` binds `ConnectionString`, `AutoDiscoverDbContexts`
and `ConnectionResolution` from `Cratis:EntityFrameworkCore`; the options callback
runs afterward and wins. Automatic discovery still needs a nonempty configured
connection string to select the provider, even if the resolver supplies a tenant
destination later.

Arc-registered contexts resolve for the current tenant and purpose whenever a
connection opens, including pooled contexts. A SQL Server access token becomes
`SqlConnection.AccessToken`; PostgreSQL uses it as the password. Do not capture
one tenant's resolved connection into singleton options or manually reuse it
across tenants.

`ConnectionPurposeScope.Begin(ConnectionPurpose.Schema)` selects schema-purpose
resolution for migrations; runtime data access uses `Runtime`. Dispose the scope
after the schema operation. Different purposes may use different identities,
but they must resolve the intended destination consistently.

## Verify

Run the application's database integration specs. Open connections for two
tenants through the same pool and assert each reaches only its own database;
exercise a schema-purpose open, credential expiry and login-failure invalidation.
Compilation alone cannot prove tenant isolation or resolver authorization.
