<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/tenancy.md -->
# Tenancy

## Contents

- Constants
- Options and defaults
- The resolvers
- Reading the tenant
- Development listing
- Where the boundary is

Verified against `Cratis.Arc.Core` `22.75.0`. Types are in `Cratis.Arc.Tenancy`.

Arc tenancy is **request-scoped tenant resolution**. It answers "which tenant is
this request for". It is not Chronicle's tenant namespace isolation, and it does
not by itself scope any storage.

## Constants

```csharp
public const string DefaultTenantIdHeader = "x-cratis-tenant-id";
public const string TenantIdItemKey       = "TenantId";
public const string DefaultFixedTenantId  = "development";
```

## Options and defaults

`ArcOptions.Tenancy` is a `TenancyOptions`:

| Property | Default |
| --- | --- |
| `ResolverType` | `TenantResolverType.Header` |
| `HttpHeader` | `x-cratis-tenant-id` |
| `BaseDomain` | empty |
| `QueryParameter` | `tenantId` |
| `ClaimType` | `tenant_id` |
| `FixedTenantId` | `development` |
| `DevelopmentTenantId` | an alias property over `FixedTenantId` |

`TenantResolverType` is `Header = 0`, `Query = 1`, `Claim = 2`,
`Development = 3`, `Subdomain = 4`, `Fixed = 5`. **Header is the default.**

Configure through the extension methods on `ArcOptions`: `UseHeaderTenancy`,
`UseQueryTenancy`, `UseClaimTenancy`, `UseDevelopmentTenancy`, `UseFixedTenancy`,
and `UseSubdomainTenancy(baseDomain, fallbackHeaderName?)`. Options are validated
by `TenancyOptionsValidator`.

## The resolvers

| Resolver | Reads |
| --- | --- |
| `HeaderTenantIdResolver` | `Tenancy.HttpHeader` |
| `QueryTenantIdResolver` | `Tenancy.QueryParameter` |
| `ClaimTenantIdResolver` | the first claim of `Tenancy.ClaimType` |
| `SubdomainTenantIdResolver` | the host label in front of `BaseDomain` |
| `FixedTenantIdResolver` | `Tenancy.FixedTenantId` |
| `DevelopmentTenantIdResolver` | deprecated alias over `FixedTenantIdResolver` |

⚠️ `SubdomainTenantIdResolver` **falls back to the configured HTTP header** when
the host does not match. A request that misses the subdomain can therefore still
name its own tenant through a header it controls. Clear `Tenancy.HttpHeader` if
that fallback is not wanted.

Its suffix is built once from the base domain at construction and never rebuilt.
Writing `BaseDomain` after construction does not take effect — and would silently
send every request down the header fallback. `BaseDomainIsNotADomainName` guards
the configured value.

## Reading the tenant

`ITenantIdAccessor.Current` gives the current `TenantId`, cached into an
`AsyncLocal`. An empty resolution is `TenantId.NotSet`, whose value is the
literal string `"[NotSet]"`. `TenantId.Default` is `"Default"`. ⚠️ `IsDefault`
is `this == NotSet || this == Default` — it is true for an **unresolved** tenant
as well, so it cannot tell "single-tenant default" from "nothing resolved".
Arc for .NET has no tenancy-required switch. Empty header/query resolution
becomes `NotSet`; Chronicle's `TenantNamespaceResolver` maps `IsDefault` (both
`NotSet` and `Default`) to its default namespace. Refuse unresolved tenants in
application authorization when required; resolution alone is not isolation.
Sources at Arc v22.75.0: `Arc.Core/Tenancy/TenancyOptions.cs` and
`Chronicle/Tenancy/TenantNamespaceResolver.cs` under `Source/DotNET`.
Compare against `TenantId.NotSet` explicitly, or use
`CurrentTenantIdIsNotSet.ThrowIfNotSet(...)`, wherever an unresolved tenant must
be refused.

⚠️ `NotSet` is a real string, not `null`. A check like
`string.IsNullOrEmpty(tenantId)` never fires for an unresolved tenant. Compare
against `TenantId.NotSet`.

`ITenantScope.Begin(TenantId)` selects a tenant explicitly for the current
asynchronous flow until the returned scope is disposed, and takes precedence over
the configured resolver. It rejects a null or blank tenant, and it does **not**
authorize access to that tenant. Resolve tenant-scoped services in a DI scope
created after beginning it.

## Development listing

`ICanProvideTenants` (`Task<IEnumerable<Tenant>> Provide()`) feeds
`GET /.cratis/tenants`, where `Tenant` is `record (TenantId Id, TenantName Name)`.
The endpoint follows `Introspection.IdentityDiscovery` and discovery access:
anonymous only in Development by default, authenticated elsewhere, or unmapped
when the host cannot authenticate. Return development fixtures only.
Source: [Arc v22.75.0 secure defaults](https://github.com/Cratis/Arc/blob/v22.75.0/Documentation/upgrading/secure-defaults.mdx).

## Where the boundary is

Tenant resolution reads a value the **caller** supplied — a header, a query
parameter, a claim, or a host. Only the claim resolver reads something the
authentication step produced. The other four are caller-controlled input.

Anything that must be isolated per tenant has to enforce that isolation itself,
downstream of resolution. Never treat a resolved tenant id as an authorization
decision.
