<!-- cratis-ai-managed: skills/cratis-arc-configuration/references/arc-options.md -->
# Arc options

## Contents

- Core
- Commands and queries
- Routes
- Identity, tenancy and discovery
- Settings that are code-only
- Names to prefer

All keys are under `Cratis:Arc`.

## Core

| Key | Default | Change it when |
| --- | --- | --- |
| `ExposeExceptionDetails` | `true` only when `ASPNETCORE_ENVIRONMENT` is `Development`, otherwise `false` | Diagnosing a deployed failure briefly; leave `false` in production. Read once at process start. |
| `CorrelationId:HttpHeader` | `X-Correlation-ID` | A gateway uses another correlation header. |
| `Calendar:TimeZone` | `UTC` | The application calendar follows a local zone. Use an IANA id; an unknown id stops the host. |
| `Hosting:ApplicationUrl` | `http://+:5001/` | Arc.Core host only. The default binds every interface; use `http://localhost:5001/` for local-only. |
| `IdentityDetailsProvider` | not set (discovered) | Discovery finds none or several providers. Code-only in practice. |

## Commands and queries

| Key | Default | Change it when |
| --- | --- | --- |
| `Command:MaximumCallerAllowedSeverity` | `Warning` | Callers may allow a different severity through the severity header. |
| `Command:IgnoreCallerAllowedSeverity` | `false` | The caller header must be ignored entirely. |
| `Query:KeepAliveInterval` | 30 seconds | A proxy closes idle observable-query connections sooner. Zero or negative disables it. |
| `Query:AllowedWebSocketOrigins` | not set (accepts any origin) | Production: set to the front-end origins. |
| `QueryHealth:RequireAuthentication` | unset: anonymous in Development, authenticated elsewhere | Require sign-in in Development too. |
| `QueryHealth:Roles` | not set | Restrict the query-health feed to roles. |

`CommandOperationOptions.CompensationTimeout` (30 seconds) is not bound from
`Cratis:Arc`; set it with `services.Configure<CommandOperationOptions>(...)`.

## Routes

| Key | Default | Must match build property |
| --- | --- | --- |
| `GeneratedApis:RoutePrefix` | `api` | `CratisProxiesApiPrefix` |
| `GeneratedApis:SegmentsToSkipForRoute` | `0` | `CratisProxiesSegmentsToSkip` |
| `GeneratedApis:IncludeCommandNameInRoute` | `true` | inverse of `CratisProxiesSkipCommandNameInRoute` |
| `GeneratedApis:IncludeQueryNameInRoute` | `true` | inverse of `CratisProxiesSkipQueryNameInRoute` |
| `GeneratedApis:EnableQueryHttpMethod` | `true` | Set `false` when infrastructure rejects the HTTP `QUERY` method. |
| `EndpointGroups:Ports`, `EndpointGroups:RoutePrefixes` | empty | Serve endpoint groups on separate ports or prefixes. |
| `EndpointGroups:ServeDefaultGroupOnGroupPorts` | `false` | The default group must also be reachable on group ports. |

## Identity, tenancy and discovery

| Key | Default | Change it when |
| --- | --- | --- |
| `TrustForwardedIdentityHeaders` | `false` | Every request comes through an ingress that authenticates callers and replaces the `x-ms-client-principal*` headers. Until then `AddCratis()` leaves every request anonymous and logs a startup warning. |
| `Introspection:Enabled` | `true` | `false` on deployed hosts that need no catalogs. |
| `Introspection:IdentityDiscovery` | `true` | `false` when no user and tenant lists are needed. |
| `Introspection:RequireAuthentication` | unset: anonymous in Development, authenticated elsewhere | Leave unset unless you must change it. |
| `Introspection:Roles` | not set | Restrict discovery to roles. |
| `Tenancy:ResolverType` | `Header` | Tenant comes from a query parameter, claim, subdomain or fixed value. |
| `Tenancy:HttpHeader` | `x-cratis-tenant-id` | A gateway uses another tenant header. |
| `Tenancy:BaseDomain` | empty | Required for the `Subdomain` resolver. The `Host` header is caller-set, so validate it upstream. |
| `Tenancy:QueryParameter` | `tenantId` | Query resolver parameter name. |
| `Tenancy:ClaimType` | `tenant_id` | Claim resolver claim name. |
| `Tenancy:FixedTenantId` | `development` | Tenant for the `Fixed` and `Development` resolvers. |
| `Tenancy:IngressValidatesTenant` | `false` | A trusted ingress validates caller-to-tenant membership. It only silences a startup diagnostic. |
| `Tenancy:StrictValidatorMembership` | `false` | Recommended `true` for new applications. |
| `ConnectionResolution:*` | resolver `Default`, cache 5 minutes | Per-tenant database connections. The same tree exists under MongoDB and EF Core. |
| `OnBehalfOf:*`, `ForwardedIdentity:*` | `Enabled` is `false` | Signed assertions or forwarded identity are in use. |

Discovery access and query-health access check the host environment first
(`DOTNET_ENVIRONMENT` counts), then `ASPNETCORE_ENVIRONMENT` when no host
environment is registered. Introspection is anonymous only in Development;
outside Development it needs a signed-in user unless `Roles` narrows it or the
feature is off.

## Settings that are code-only

These have no `Cratis:Arc` section; set them in code:
`CapabilitiesOptions` (`EndpointGroup` default `capabilities`, `GatedEndpoints`,
`AllowedPrincipalKinds`), MCP options through `WithMcp(...)` (`Path` default
`/mcp`, `Profile` default `PatOnly`, `MaxRequestBodySize` 1 MiB,
`ExclusiveTools` default `true`), `ProtectedReadModelKeyOptions.Strict` (default
`false`). Setting `ExclusiveTools` to `false` serves other registered tools with
no scope check.

## Names to prefer

| Setting | Prefer | Also accepted |
| --- | --- | --- |
| Fixed tenant | `Tenancy:FixedTenantId` | `Tenancy:DevelopmentTenantId` (sets both) |
| Trust unsigned identity headers | `TrustForwardedIdentityHeaders` | `Introspection:TrustForwardedIdentityHeaders` (obsolete) |
| Register MongoDB | `WithMongoDB()` | `UseCratisMongoDB()`, `AddCratisMongoDB()`; register once |
| React base path | `apiBasePath` | `basePath` does nothing |
