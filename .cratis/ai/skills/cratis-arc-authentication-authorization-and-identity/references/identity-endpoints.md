<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/identity-endpoints.md -->
# Identity and discovery endpoints

This reference holds the `/.cratis` endpoint table, discovery exposure settings and identity response behavior.

## The endpoints

| Route | What it does |
| --- | --- |
| `GET /.cratis/me` | The current identity. 401 when not authenticated, 403 when the provider says not authorized, otherwise the principal-derived identity JSON |
| `GET /.cratis/identity-details/schema` | The JSON schema of the details type (`{}` without a details provider) |
| `GET /.cratis/users` | Development tooling — aggregates every `ICanProvideUsers` |
| `GET /.cratis/tenants` | Development tooling — aggregates every `ICanProvideTenants` |
| `GET /.cratis/commands`, `GET /.cratis/queries` | Introspection catalogs of operation names, types and routes (`MapIntrospectionEndpoints()`) |

## Discovery exposure and identity responses

Discovery is anonymous only when the host's
`IHostEnvironment.IsDevelopment()` is true. Elsewhere it requires authentication;
if the host cannot authenticate callers, the routes are not mapped (404) and a
warning is logged. `RequireAuthentication` unset selects that environment default;
explicit `true` requires authentication everywhere (startup fails if unavailable);
explicit `false` exposes discovery anonymously everywhere, warning outside
Development. `Roles` implies authentication and cannot combine with explicit
`false`. `Enabled=false` removes the two catalogs; `IdentityDiscovery=false`
removes users, tenants and the details schema. Setting both removes all discovery
without authentication checks; `/.cratis/me` remains. `[ExcludeFromDiscovery]`
hides commands, read models, queries and controllers from catalogs/OpenAPI, **not
from invocation or authorization**. These are .NET settings, not a JVM contract.
Source: [secure defaults](https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.mdx).

`/.cratis/me` derives identity only from the authenticated request principal,
never from `.cratis-identity`. The details provider runs for each authenticated
identity request; `ModifyDetails` changes only the current response. Arc no
longer writes the readable identity cookie (it expires a legacy one). Identity
responses carry `Cache-Control: no-store, private` and `Vary: Cookie`.
Stop reading the cookie; use `IdentityProvider.getCurrent()`/`refresh()` and
`clearCache()` on logout. The frontend caches in memory, with a warned legacy
cookie fallback **only after a 404**, never after 401/403.
Sources: [IdentityProvider.cs](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Identity/IdentityProvider.cs),
[IdentityProvider.ts](https://github.com/Cratis/Arc/blob/main/Source/JavaScript/Arc/identity/IdentityProvider.ts).
