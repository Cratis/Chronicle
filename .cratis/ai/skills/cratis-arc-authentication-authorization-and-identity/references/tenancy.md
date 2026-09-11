<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/tenancy.md -->
# Tenancy

Verified against `Cratis.Arc.Core` `22.10.4`. Types are in `Cratis.Arc.Tenancy`.

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
literal string `"[NotSet]"`. `TenantId.Default` is `"Default"` and `IsDefault`
tests for it. `CurrentTenantIdIsNotSet.ThrowIfNotSet(...)` is the guard for code
that requires a tenant.

⚠️ `NotSet` is a real string, not `null`. A check like
`string.IsNullOrEmpty(tenantId)` never fires for an unresolved tenant. Compare
against `TenantId.NotSet`.

## Development listing

`ICanProvideTenants` (`Task<IEnumerable<Tenant>> Provide()`) feeds
`GET /.cratis/tenants`, where `Tenant` is `record (TenantId Id, TenantName Name)`.
The endpoint is `AllowAnonymous` and mapped unconditionally, so an implementation
that returns real tenants publishes them. Return development fixtures only.

## Where the boundary is

Tenant resolution reads a value the **caller** supplied — a header, a query
parameter, a claim, or a host. Only the claim resolver reads something the
authentication step produced. The other four are caller-controlled input.

Anything that must be isolated per tenant has to enforce that isolation itself,
downstream of resolution. Never treat a resolved tenant id as an authorization
decision.
