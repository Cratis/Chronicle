<!-- cratis-ai-managed: skills/cratis-arc-configuration/references/symptoms.md -->
# Symptoms and the setting behind them

Each row is a first hypothesis; confirm before changing.

| Symptom | Setting to check |
| --- | --- |
| Build succeeds but no TypeScript proxies appear | `CratisProxiesOutputPath` is empty, or `-p:CratisProxiesSkip=true` is set; the generator target runs only when the path is set and Skip is not `true` |
| Generated client calls return 404 | Generator and host route settings differ: prefix, segments to skip, name inclusion (skip and include are inverses) |
| Requests go to the wrong path behind a gateway | `apiBasePath` duplicated `/api` or missing the deployment prefix; `origin` bypassing the dev proxy |
| Every request is anonymous after `AddCratis()`; endpoints return 401 | `TrustForwardedIdentityHeaders` is `false` by default; enable only behind an ingress that authenticates and replaces the headers |
| Exception details missing in a Development-like environment | `ExposeExceptionDetails` follows `ASPNETCORE_ENVIRONMENT` only; `DOTNET_ENVIRONMENT` does not set it |
| Exception details visible in production | `ExposeExceptionDetails` set to `true`; remove it |
| Discovery or `/.cratis/me` returns 401 or 404 | Introspection is anonymous only in Development; outside it needs a signed-in user unless `Introspection:Roles`, `Enabled` or `IdentityDiscovery` changes that; check the frontend `apiBasePath` and origin |
| Query-health feed refused | `QueryHealth:RequireAuthentication`, `QueryHealth:Roles` |
| Observable query WebSocket connections accepted from any origin | `Query:AllowedWebSocketOrigins` unset; set the front-end origins |
| Observable query connection dropped by a proxy while idle | `Query:KeepAliveInterval` |
| Host fails at startup with a time zone error | `Calendar:TimeZone` must be a valid IANA id |
| Host fails at startup with a MongoDB error | `Cratis:MongoDB:Server` and `Database` are required |
| `DbContext` types not discovered | `Cratis:EntityFrameworkCore:ConnectionString` empty, or `AutoDiscoverDbContexts` off |
| Tenant resolved wrongly | `Tenancy:ResolverType`, header, claim or query names; header, query and subdomain resolvers read caller-set values; trust them only behind a gateway that sets them |
| Read-protection settings seem ignored | Keys must be root-level `ReadProtection`, not under `Cratis:Arc`; the mode is the `CratisReadProtection` build property |
| MCP tools served without a scope check | `ExclusiveTools` set to `false` |
| Setting in appsettings has no effect | A `configureOptions` callback runs after binding and wins; or the key is code-only |
