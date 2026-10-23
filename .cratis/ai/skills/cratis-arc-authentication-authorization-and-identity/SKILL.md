---
name: cratis-arc-authentication-authorization-and-identity
description: Wire authentication, authorization and identity in a Cratis Arc application — IProvideIdentityDetails and the /.cratis/me endpoint, Arc's [Authorize]/[Roles]/[AllowAnonymous] attributes and named policies, fallback baselines, observable-query emission guards, IAuthenticationHandler, the Microsoft Identity Platform header contract and its ingress requirements, discovery and introspection exposure, tenant resolution, and the React identity hooks. Use for Arc identity providers, endpoint protection, roles, policies, or frontend identity integration. Do not use for isolated command validation, and do not use for Chronicle tenant namespaces alone.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/SKILL.md -->

# Authentication, authorization and identity in Arc

Arc separates three things that are easy to conflate: **authentication** decides
who the caller is, **authorization** decides whether that caller may run this
command or query, and the **identity provider** decides what the frontend is
told about them. Each has its own extension point.

## Verified product sources

Read [references/product-sources.md](references/product-sources.md) when you need the owning package for an identity type.

Authorization capabilities arrived over time: named policies, schemes, fallback baselines, guest policies and Microsoft-attribute enforcement all need a recent Arc. [authorization](references/authorization.md) lists the requirements; check them if you run an older version.

## Read this first: what is and is not trusted

Arc's shipped authentication reads a **forwarded** principal from HTTP headers.
`x-ms-client-principal` is base64, **not a signature**, and Arc does not check
who sent it. Any caller that can reach the application can author the entire
serialized principal.

That is not a defect — it is the EasyAuth/reverse-proxy model, where the ingress
is the trust boundary. But it means:

- the application must be unreachable except through the ingress that sets those
  headers, and that ingress must **authenticate callers, strip any
  caller-supplied identity headers, write its own trusted replacements, and block
  direct access to the backend**;
- nothing Arc gives you makes the forwarded values authentic;
- Arc ships **no** OpenID Connect and **no** JWT bearer integration. If you need
  the application itself to validate a token, that is ordinary ASP.NET Core
  authentication you configure yourself.

Requires Arc 22.43.0 for secure forwarded-header handling. On an older Arc the unsigned identity headers are trusted from any caller who can reach the host; upgrade, or make the ingress the only path to the backend.

Forwarded identity headers are ignored unless the host opts
in with `Cratis:Arc:TrustForwardedIdentityHeaders=true` or
`builder.AddCratis(options => options.TrustForwardedIdentityHeaders = true)`.
Header-only requests otherwise remain anonymous. `AddCratis()` still registers
the header scheme; it does not make the headers trusted. Opt in only behind the
ingress above, otherwise configure a credential-validating scheme. The old
`Introspection:TrustForwardedIdentityHeaders` setting is obsolete and enables the
same host-wide flag. See [authentication](references/authentication.md).
Source: [secure defaults](https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.mdx).

Cratis AuthProxy is the ingress that meets these obligations for Cratis applications. Read [AuthProxy](references/authproxy.md) when the application is deployed behind it (header contract, `/.cratis/me` fail-closed verification, tenant headers).

## Identity details — what the frontend is told

Implement `IProvideIdentityDetails` to shape what `/.cratis/me` returns:

```csharp
using Cratis.Arc.Identity;

public class <Name>IdentityDetailsProvider : IProvideIdentityDetails<<Details>>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        // context.Id, context.Name, context.Claims
        return new IdentityDetails(<isAuthorized>, new <Details>(<...>));
    }
}
```

- `IdentityDetails` is `record IdentityDetails(bool IsUserAuthorized, object Details)`.
  `Details` is **`object`**, not generic; it is serialized as JSON.
- `IProvideIdentityDetails<TDetails>` adds no members. It exists only so Arc can
  capture the details type reflectively, which is what feeds the JSON schema
  endpoint and the generated proxy. Implement **both** — the generic one for the
  type, the non-generic `Provide` for the behavior.
- `IdentityProviderContext` is `(IdentityId Id, IdentityName Name, IEnumerable<KeyValuePair<string, string>> Claims)`.
- Registration is by discovery: exactly one non-default implementation is
  expected. More than one is `MultipleIdentityDetailsProvidersFound`; none falls
  back to `DefaultIdentityDetailsProvider`, which returns
  `new IdentityDetails(true, new { })`. The provider is registered **scoped**.

### The endpoints

Read [references/identity-endpoints.md](references/identity-endpoints.md) when choosing what discovery routes a host exposes or how `/.cratis/me` and its caching behave.

Requires Arc 22.45.0 for authenticated discovery by default. On an older Arc the discovery routes are anonymous in every environment, so do not expose such a host.

## Authorization — what Arc evaluates

Arc's attributes live in `Cratis.Arc.Authorization`:

| Attribute | Targets | Notes |
| --- | --- | --- |
| `[AllowAnonymous]` | class, method | No arguments |
| `[Authorize]` | class, method, repeatable | `Policy`, `Roles` (comma-delimited), `AuthenticationSchemes` |
| `[Roles(params string[] roles)]` | class, method | Derives from `[Authorize]`, joins the roles with commas |

There is **no** `[Policy]`, `[Scopes]` or `[Claims]` attribute anywhere in Arc.
Arc's attributes are the portable choice: they are enforced on every Arc host.

Requires Arc 22.23.0 for named policies and schemes, and 22.21.0 for Microsoft's `[Authorize]`/`[AllowAnonymous]` enforcement (ASP.NET Core host only, never the standalone Core host). On an older Arc only `Roles` is evaluated: `[Authorize(Policy = "…")]` and `AuthenticationSchemes` enforce nothing, and a command protected only by Microsoft's `[Authorize]` is open. See [authorization](references/authorization.md) for the requirements.

Current behavior:

- **Named policies work.** `[Authorize(Policy = "…")]` requires the policy to
  succeed asynchronously. Implement
  `IAuthorizationPolicy.IsAuthorized(AuthorizationPolicyContext, CancellationToken)`
  and register it with `AddArcAuthorizationPolicy<T>(name)` before `Build()`.
  Unknown or duplicate names fail startup; an unresolved policy never grants access.
  The context carries `Principal`, `Target`, `Resource` and `ReceivedAt`.
  The old synchronous `IAuthorizationEvaluator.IsAuthorized` rejects policy-bearing
  declarations.
- **Schemes.** `AuthenticationSchemes` is honored on the ASP.NET Core host and
  rejected at startup on the standalone Core host.
- **Composition.** Stacked attributes **AND**; only a role list is OR. A method's
  declaration **replaces** its type's — a method-level `[AllowAnonymous]` or
  guest policy removes a type-level `[Roles]` for that method. `[Authorize]` plus
  `[AllowAnonymous]` on one member throws `AmbiguousAuthorizationLevel`.
- **Guests.** A policy requires an authenticated principal by default. Unauthenticated
  callers are evaluated only for policies registered with
  `AddArcAuthorizationPolicy<T>(name, evaluatesAnonymous: true)`, or ASP.NET policies
  opted in with `AddArcAnonymousAspNetAuthorizationPolicy(name)` (exact, case-sensitive
  name; no authenticated-user requirement, no schemes). Roles and schemes always
  require authentication.
- **Order.** Policies are awaited **before** `Provide()`, `Handle()` or the query
  method; context-value providers and scope `Begin` run before the verdict;
  identity and declaration continuity is rechecked around each policy and before
  the handler. A change denies. Policy checks gate **admission**; they do not revoke.
- **Baselines.** `IFallbackAuthorizationEvaluator` supplies a baseline for commands
  and queries with no explicit declaration, on both hosts, and ASP.NET's
  `FallbackPolicy` covers HTTP endpoints and MVC actions. They are separate:
  neither covers what the other does, and neither protects an explicitly anonymous
  endpoint.
- **Direct calls bypass authorization.** Calling `Handle()` or a query method
  directly skips authorization, validation and filters; only the pipeline and
  mapped endpoints enforce them. Server-side callers still need a principal
  (`ICurrentPrincipalAccessor`, `ISystemExecution`).
- **Results.** A denied command returns `CommandResult.Unauthorized(context.CorrelationId)`,
  a query `QueryResult.Unauthorized(...)`; mapped Arc denials are 403, middleware may
  answer 401 or another shape.

`ARC0011` warns on a `[Roles]` string literal (use `nameof`); the `ARC0019`–`ARC0021` diagnostics are explained in [authorization](references/authorization.md#analyzer-diagnostics).

### Observable queries and revocation

Authorization runs when a subscription is established. Nothing ends a running
stream when a token expires, a session is signed out or a role changes. Implement
`IGuardObservableQueryEmission` (discovered by convention, no registration) to
re-check access on **every emission on every transport** (hub, WebSocket, SSE):
the verdict is `Allow`, `Suppress` (withheld, delta baseline unmoved) or
`DenyAndTerminate` (that subscription only; the client stops reconnecting). The most
restrictive verdict wins, and a guard that throws, or cannot be constructed, fails
closed — the latter denies every observable query in the process. The principal comes
from `ObservableQueryEmissionContext.Principal`, never from ambient accessors or
`IHttpContextAccessor`; the guard is constructed per emission, so keep it cheap and
cache in a singleton. Requires Arc 22.69.1 for guards and read-model interceptors to also
protect plain HTTP snapshots, waits and long polls; older versions leave them unguarded. Read
[observable emission guards](references/observable-emission-guards.md) when implementing or reviewing revocation of an observable-query subscription.

### Validators are not an authorization boundary

Authorization runs before validation, so a caller who may not act never reaches a
validator. Express who may act through policies, roles and `[Authorize]`, not a
validator. A state rule ("the order is still open") belongs in a
`CommandValidator<T>` with the read model as a constructor parameter; under read
protection `On` that read guards the append, under `Off` (the default) it is a
hint, and in an `[Unprotected]` command it is advisory. A state-dependent
permission or ownership rule (this member owns the order) may sit there too, with
the same limit. Under `[ProtectedDecision]` without `On`, only a parameterless
validator runs; see the command-validation and Chronicle guidance.

Read [authorization](references/authorization.md) for the guest rules, evaluation
order, the two baselines, default access under Arc's two hosting models, the
filters, and system-execution.

## Authentication — the extension point

`IAuthenticationHandler` is a single method,
`Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)`.
Implement it and it is discovered — there is **no** `IArcBuilder` extension and
**no** options type for authentication.

Arc ships `MicrosoftIdentityPlatformAuthenticationHandler` in `Arc.Core`, which
reads the forwarded principal. See
[authentication](references/authentication.md) for the exact header contract,
the claim Arc reserves, and the ASP.NET Core scheme variant.

## Tenancy

Anything that must be isolated per tenant has to enforce that isolation itself,
downstream of resolution. Never treat a resolved tenant id as an authorization
decision.

`Cratis.Arc.Tenancy` resolves a tenant per request. The default resolver is
**header-based** on `x-cratis-tenant-id`; `TenantResolverType` also offers
`Query`, `Claim`, `Development`, `Subdomain` and `Fixed`, configured through
`ArcOptions.Tenancy` or the `Use*Tenancy` extension methods.

`ITenantIdAccessor.Current` gives the current `TenantId`, which is
`TenantId.NotSet` (`"[NotSet]"`) when nothing resolved. Read
[tenancy](references/tenancy.md) when selecting or configuring a tenant resolver, or handling an unresolved tenant; it gives the exact defaults and each resolver's
behavior.

Arc tenancy is request-scoped tenant *resolution*. It is not Chronicle's tenant
namespace isolation — that is separate guidance.

## Frontend

Read [references/frontend.md](references/frontend.md#identity-hook-examples) when you need the `<Arc>` provider and `useIdentity`/`RequireRole` examples.

`useIdentity` has two overloads: `(type, defaultDetails?)` for type-safe details
and `(defaultDetails?)` without. The default details stand in whenever there are
no details to give, not only when the identity is explicitly unset.

`RequireRole` gates rendering by `roles` or by an `allow` predicate, with
`whileLoading` and `forbidden` slots.

⚠️ **`RequireRole` hides UI; it does not protect data.** The identity it reads
comes from the principal-derived identity endpoint, but browser state is editable. Every rule it expresses
must also exist on the server as `[Authorize]`/`[Roles]` or an authorization
filter.

`Arc.React.MVVM` has **no** identity hook. It registers `IIdentityProvider` in
its container so a view model can constructor-inject it; that is all.

Read [frontend](references/frontend.md) when consuming identity in React or injecting it into a view model; it gives the exact exported shapes.

## Local development

Arc ships **no** development identity provider, no fake principal, and no local
sign-in. What exists for development is the two anonymous listing endpoints,
`/.cratis/users` and `/.cratis/tenants`, which aggregate whatever the application
implements as `ICanProvideUsers` and `ICanProvideTenants`.

To run locally with an identity, supply the forwarded headers yourself — see
[local development](references/local-development.md).

## Verify

- Exactly one non-default `IProvideIdentityDetails` implementation exists, and it
  also implements `IProvideIdentityDetails<TDetails>`.
- The Arc version in use enforces what the code declares: policies and Microsoft's
  attributes need a recent Arc (see the authorization reference).
- Every named policy is registered exactly once; a policy meant for guests has the
  explicit opt-in, and no declaration mixes a guest policy with roles or schemes.
- A method-level `[AllowAnonymous]` or guest policy is intended to replace its
  type's `[Roles]`.
- `[Roles]` arguments use `nameof`, so `ARC0011` is silent.
- No member carries both `[Authorize]` and `[AllowAnonymous]`.
- A baseline exists on purpose: an `IFallbackAuthorizationEvaluator` for the Arc
  pipeline and, on ASP.NET Core, a `FallbackPolicy` for HTTP endpoints and MVC
  actions.
- Authorization-sensitive logic is never reached by calling `Handle()` or a query
  method directly.
- Observable queries that serve private or revocable data have an emission guard
  backed by current session and membership state.
- State-dependent permission and ownership rules are guarded: read protection `On`
  with the read model in the validator or handler, or `[ProtectedDecision]` commands
  with the rule in `Provide()`/`Handle()`. Under `Off` they are hints.
- Discovery endpoints (`/.cratis/commands`, `/.cratis/queries`,
  `/.cratis/identity-details/schema`, `/.cratis/users`, `/.cratis/tenants`) are
  tested under Production settings; introspection is restricted or disabled where it
  must not be public, including identity discovery.
- `TrustForwardedIdentityHeaders` is set only behind an ingress that authenticates
  callers, strips caller-supplied identity headers, writes trusted ones and blocks
  direct backend access; custom handlers honor it.
- Every rule the frontend enforces also exists on the server.
- Nothing trusts the identity cookie or the forwarded headers beyond what the
  ingress guarantees.
- The application is not reachable except through that ingress.
- Development user and tenant providers return fixtures, never production data.
- `dotnet build` is clean in Debug and Release.

## Route near misses

- Rejecting a command's input or state: `cratis-arc-command-validation` scope
  (validators are not the place for state-dependent permission rules).
- Chronicle tenant namespaces and per-tenant event stores: the Chronicle
  multi-tenancy guidance.
- Building the React page that consumes identity: the Arc React guidance.
