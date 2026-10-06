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

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.41.1` | `Cratis.Arc.Identity`, `Cratis.Arc.Authorization`, `Cratis.Arc.Authentication`, `Cratis.Arc.Tenancy` |
| `Cratis.Arc` | `22.41.1` | ASP.NET Core hosting, `AddMicrosoftIdentityPlatformIdentityAuthentication` |
| `@cratis/arc` | `22.41.1` | `IdentityProvider`, `IIdentity` |
| `@cratis/arc.react` | `22.41.1` | `IdentityProvider` component, `useIdentity`, `RequireRole` |

> Re-verified at `22.41.1` (the latest release when written) against that tag's source and Arc's documentation: the types, attributes and members this skill names exist, and the authorization, introspection, identity and emission-guard behavior it describes matches them. **Authorization behavior changed materially after `22.16.0`**, the version this skill used to pin: named policies, schemes, fallback baselines, guest policies and Microsoft-attribute enforcement all arrived later. [authorization](references/authorization.md) has a version table; check it if you run an older version.

Reverify before claiming support for another version.

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

Today the default is that a plain `builder.AddCratis()` host registers the
unsigned-header scheme as its **default** authentication scheme, so any caller who
can reach it can forge a principal for every authorized command and query (open
Arc#2946). Run it only behind that ingress; otherwise use `AddCratisArc()` and
configure a credential-validating scheme. Arc itself refuses the unsigned headers
in one place — protected introspection — until
`Cratis:Arc:Introspection:TrustForwardedIdentityHeaders` is set; see
[authentication](references/authentication.md).

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

| Route | What it does |
| --- | --- |
| `GET /.cratis/me` | The current identity. 401 when not authenticated, 403 when the provider says not authorized, otherwise the result plus the identity cookie |
| `GET /.cratis/identity-details/schema` | The JSON schema of the details type (`{}` without a details provider) |
| `GET /.cratis/users` | Development tooling — aggregates every `ICanProvideUsers` |
| `GET /.cratis/tenants` | Development tooling — aggregates every `ICanProvideTenants` |
| `GET /.cratis/commands`, `GET /.cratis/queries` | Introspection catalogs of operation names, types and routes (`MapIntrospectionEndpoints()`) |

Normal `UseCratisArc()` activation maps all of these `AllowAnonymous` by default,
**including in Production**, unless an endpoint of the same name already exists; `/.cratis/me`
does its own 401/403. There is no public constant for the identity routes — they
are string literals in the endpoint mapper.

⚠️ **A fallback policy does not protect an endpoint that is explicitly anonymous**,
and the word "development" is not an environment check. Today the default is that
these routes are public and expose your command, query, user and tenant surface;
the exposure is tracked in open Arc#2834. To change it:

- the two catalogs: configure `Cratis:Arc:Introspection` (`Enabled: false` unmaps
  them; `RequireAuthentication`/`Roles` protect them; the
  `TrustForwardedIdentityHeaders` opt-in is for the header handler only behind a
  trusted ingress) — options table in [authentication](references/authentication.md);
- `/.cratis/identity-details/schema`, `/.cratis/users` and `/.cratis/tenants` are
  **not** affected by those options. Return fixtures only from the user and tenant
  providers, and restrict all three at the trusted ingress.

Test the discovery endpoints under Production settings, with your fallback policy
enabled.

`/.cratis/me` writes the result as JSON **and** as a base64 cookie named
`.cratis-identity`, deliberately **not `HttpOnly`** so the frontend can read it
without a round trip. `Secure` follows whether the request is HTTPS, `SameSite`
is `Lax`, `Path` is `/`.

⚠️ A non-`HttpOnly` cookie the browser can read is a cookie the user can edit.
Treat everything in it as a **display** input. Never let it decide anything the
server has not already decided. Today the default is that `/.cratis/me` returns
whatever a presented `.cratis-identity` cookie decodes to *before* it looks at the
authenticated principal, so a client can make it report any identity or roles
(open Arc#2939). Commands and queries authorize on the request principal and are
not affected; a frontend that shows or branches on `/.cratis/me` can be misled.

## Authorization — what Arc evaluates

Arc's attributes live in `Cratis.Arc.Authorization`:

| Attribute | Targets | Notes |
| --- | --- | --- |
| `[AllowAnonymous]` | class, method | No arguments |
| `[Authorize]` | class, method, repeatable | `Policy`, `Roles` (comma-delimited), `AuthenticationSchemes` |
| `[Roles(params string[] roles)]` | class, method | Derives from `[Authorize]`, joins the roles with commas |

There is **no** `[Policy]`, `[Scopes]` or `[Claims]` attribute anywhere in Arc.
Arc's attributes are the portable choice: they are enforced on every Arc host.

⚠️ **Which version you run decides what is enforced.** Through `22.20.0` (this
skill's old pin was `22.16.0`) only `Roles` is evaluated: `[Authorize(Policy = "…")]`
and `AuthenticationSchemes` enforce nothing, and Microsoft's `[Authorize]` is **not**
honored on model-bound commands and read models, so a command protected only by it
is open. From `22.21.0` Microsoft's `[Authorize]`/`[AllowAnonymous]` are enforced on
the ASP.NET Core host (never on the standalone Core host), and from `22.23.0` named
policies and schemes are evaluated. The rest of this section is `22.41.1` behavior;
see [authorization](references/authorization.md) for the version table.

At `22.41.1`:

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

`ARC0011` warns when a `[Roles]` argument is a string literal: use
`nameof(<RoleEnum>.<Member>)` so a rename is a compile error rather than a silent
authorization failure. `ARC0019` flags `[AllowAnonymous]` with `[Authorize]`/`[Roles]`,
`ARC0020` flags an ASP.NET Core attribute that is not enforced without Arc's ASP.NET
Core integration, and `ARC0021` flags `AuthenticationSchemes` on the Core host.

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
cache in a singleton. Guards do not protect the HTTP snapshot path. Read
[observable emission guards](references/observable-emission-guards.md).

### Validators are not an authorization boundary

A `[ProtectedDecision]` command runs only certified, parameterless validators and
refuses validators with constructor dependencies. A permission or ownership rule
that depends on state does not belong in a validator (or in an `[Unprotected]`
one): put it in `Provide()`/`Handle()` with `DecisionRead<T>` or `IDecisionReads`,
and see the command-validation and Chronicle guidance.

Read [authorization](references/authorization.md) for the guest rules, evaluation
order, the two baselines, default access under Arc's two hosting models, the
filters, and system-execution.

## Authentication — the extension point

`IAuthenticationHandler` is a single method,
`Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)`.
Implement it and it is discovered — there is **no** `IArcBuilder` extension and
**no** options type for authentication.

`AuthenticationResult` is
`record (ClaimsPrincipal? Principal = default, AuthenticationFailure? Failure = default)`
with `Anonymous`, `Succeeded(principal)` and `Failed(reason)`, and
`IsAuthenticated => Principal is not null`.

Arc ships `MicrosoftIdentityPlatformAuthenticationHandler` in `Arc.Core`, which
reads the forwarded principal. See
[authentication](references/authentication.md) for the exact header contract,
the claim Arc reserves, and the ASP.NET Core scheme variant.

## Tenancy

`Cratis.Arc.Tenancy` resolves a tenant per request. The default resolver is
**header-based** on `x-cratis-tenant-id`; `TenantResolverType` also offers
`Query`, `Claim`, `Development`, `Subdomain` and `Fixed`, configured through
`ArcOptions.Tenancy` or the `Use*Tenancy` extension methods.

`ITenantIdAccessor.Current` gives the current `TenantId`, which is
`TenantId.NotSet` (`"[NotSet]"`) when nothing resolved. See
[tenancy](references/tenancy.md) for the exact defaults and each resolver's
behavior.

Arc tenancy is request-scoped tenant *resolution*. It is not Chronicle's tenant
namespace isolation — that is separate guidance.

## Frontend

```tsx
import { Arc } from '@cratis/arc.react';

<Arc detailsType={<Details>}>{children}</Arc>
```

`<Arc>` renders the `IdentityProvider` context for you. Inside it:

```tsx
import { useIdentity, RequireRole } from '@cratis/arc.react/identity';

const identity = useIdentity(<Details>, <defaultDetails>);
// identity.id, .name, .roles, .details, .isSet, .isInRole(role)
// identity.isLoading, identity.clearIdentity()
```

`useIdentity` has two overloads: `(type, defaultDetails?)` for type-safe details
and `(defaultDetails?)` without. The default details stand in whenever there are
no details to give, not only when the identity is explicitly unset.

`RequireRole` gates rendering by `roles` or by an `allow` predicate, with
`whileLoading` and `forbidden` slots.

⚠️ **`RequireRole` hides UI; it does not protect data.** The identity it reads
comes from the deliberately non-`HttpOnly` cookie above. Every rule it expresses
must also exist on the server as `[Authorize]`/`[Roles]` or an authorization
filter.

`Arc.React.MVVM` has **no** identity hook. It registers `IIdentityProvider` in
its container so a view model can constructor-inject it; that is all.

See [frontend](references/frontend.md) for the exact exported shapes.

## Local development

Arc ships **no** development identity provider, no fake principal, and no local
sign-in. What exists for development is the two anonymous listing endpoints,
`/.cratis/users` and `/.cratis/tenants`, which aggregate whatever the application
implements as `ICanProvideUsers` and `ICanProvideTenants`.

To run locally with an identity, supply the forwarded headers yourself — see
[local development](references/local-development.md).

⚠️ Those two endpoints are `AllowAnonymous` and mapped unconditionally, in
Production too, and a fallback policy does not protect them. If your
`ICanProvideUsers` implementation returns real users, that list is public. Return
development fixtures only, and gate the implementation on a development
environment check.

## Verify

- Exactly one non-default `IProvideIdentityDetails` implementation exists, and it
  also implements `IProvideIdentityDetails<TDetails>`.
- The Arc version in use enforces what the code declares: policies and Microsoft's
  attributes are not enforced before `22.23.0` and `22.21.0` respectively.
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
- Permission and ownership rules in `[ProtectedDecision]` commands live in
  `Provide()`/`Handle()`, not in validators.
- Discovery endpoints (`/.cratis/commands`, `/.cratis/queries`,
  `/.cratis/identity-details/schema`, `/.cratis/users`, `/.cratis/tenants`) are
  tested under Production settings; introspection is restricted or disabled where it
  must not be public, and the rest are restricted at the ingress.
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
