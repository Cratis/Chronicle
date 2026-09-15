---
name: cratis-arc-authentication-authorization-and-identity
description: Wire authentication, authorization and identity in a Cratis Arc application — IProvideIdentityDetails and the /.cratis/me endpoint, the [Authorize]/[Roles]/[AllowAnonymous] attributes Arc actually evaluates, IAuthenticationHandler, the Microsoft Identity Platform header contract, tenant resolution, and the React identity hooks. Use for Arc identity providers, endpoint protection, roles, or frontend identity integration. Do not use for isolated command validation, and do not use for Chronicle tenant namespaces alone.
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
| `Cratis.Arc.Core` | `22.10.4` | `Cratis.Arc.Identity`, `Cratis.Arc.Authorization`, `Cratis.Arc.Authentication`, `Cratis.Arc.Tenancy` |
| `Cratis.Arc` | `22.10.4` | ASP.NET Core hosting, `AddMicrosoftIdentityPlatformIdentityAuthentication` |
| `@cratis/arc` | `22.10.4` | `IdentityProvider`, `IIdentity` |
| `@cratis/arc.react` | `22.10.4` | `IdentityProvider` component, `useIdentity`, `RequireRole` |

Reverify before claiming support for another version.

## Read this first: what is and is not trusted

Arc's shipped authentication reads a **forwarded** principal from HTTP headers.
`x-ms-client-principal` is base64, **not a signature**, and Arc does not check
who sent it. Any caller that can reach the application can author the entire
serialized principal.

That is not a defect — it is the EasyAuth/reverse-proxy model, where the ingress
is the trust boundary. But it means:

- the application must be unreachable except through the ingress that sets those
  headers;
- nothing Arc gives you makes the forwarded values authentic;
- Arc ships **no** OpenID Connect and **no** JWT bearer integration. If you need
  the application itself to validate a token, that is ordinary ASP.NET Core
  authentication you configure yourself.

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
| `GET /.cratis/identity-details/schema` | The JSON schema of the details type |
| `GET /.cratis/users` | Development tooling — aggregates every `ICanProvideUsers` |
| `GET /.cratis/tenants` | Development tooling — aggregates every `ICanProvideTenants` |

All four are mapped `AllowAnonymous`; `/.cratis/me` does its own 401/403. There
is no public constant for these routes — they are string literals in the endpoint
mapper.

`/.cratis/me` writes the result as JSON **and** as a base64 cookie named
`.cratis-identity`, deliberately **not `HttpOnly`** so the frontend can read it
without a round trip. `Secure` follows whether the request is HTTPS, `SameSite`
is `Lax`, `Path` is `/`.

⚠️ A non-`HttpOnly` cookie the browser can read is a cookie the user can edit.
Treat everything in it as a **display** input. Never let it decide anything the
server has not already decided.

## Authorization — what Arc actually evaluates

Arc ships exactly three attributes, all in `Cratis.Arc.Authorization`:

| Attribute | Targets | Notes |
| --- | --- | --- |
| `[AllowAnonymous]` | class, method | No arguments |
| `[Authorize]` | class, method, repeatable | Settable `Policy`, `Roles` (comma-delimited), `AuthenticationSchemes` |
| `[Roles(params string[] roles)]` | class, method | Derives from `[Authorize]`, joins the roles with commas |

There is **no** `[Policy]`, `[Scopes]` or `[Claims]` attribute anywhere in Arc.

⚠️ **`AuthorizeAttribute.Policy` and `AuthenticationSchemes` are declared but
never read by Arc's evaluator.** Writing `[Authorize(Policy = "…")]` compiles and
enforces nothing. Only `Roles` is evaluated.

Evaluation, in order:

1. every `IAnonymousEvaluator` is consulted; the first non-null answer decides
   anonymous access;
2. every `IAuthorizationAttributeEvaluator` is consulted for `[Authorize]` and
   its `Roles`;
3. no `[Authorize]` at all means **authorized**; with `[Authorize]`, a null or
   unauthenticated principal fails, and a non-empty role list passes when the
   user is in **any** of them (comma-split and trimmed — OR, not AND);
4. a method with no `[Authorize]` falls back to its declaring type's verdict.

Both `[Authorize]` and `[AllowAnonymous]` on the same member throws
`AmbiguousAuthorizationLevel`.

`ARC0011` warns when a `[Roles]` argument is a string literal: use
`nameof(<RoleEnum>.<Member>)` so a rename is a compile error rather than a silent
authorization failure — a desynchronized literal either locks the endpoint or
matches a stale role.

Read [authorization](references/authorization.md) for the default-access
question, which is genuinely different between Arc's two hosting models, and for
the filter and system-execution extension points.

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

⚠️ Those two endpoints are `AllowAnonymous` and mapped unconditionally. If your
`ICanProvideUsers` implementation returns real users, that list is public. Return
development fixtures only, and gate the implementation on a development
environment check.

## Verify

- Exactly one non-default `IProvideIdentityDetails` implementation exists, and it
  also implements `IProvideIdentityDetails<TDetails>`.
- No authorization decision depends on `[Authorize(Policy = …)]`.
- `[Roles]` arguments use `nameof`, so `ARC0011` is silent.
- No member carries both `[Authorize]` and `[AllowAnonymous]`.
- Every rule the frontend enforces also exists on the server.
- Nothing trusts the identity cookie or the forwarded headers beyond what the
  ingress guarantees.
- The application is not reachable except through that ingress.
- Development user and tenant providers return fixtures, never production data.
- `dotnet build` is clean in Debug and Release.

## Route near misses

- Rejecting a command's input or state: `cratis-arc-command-validation` scope.
- Chronicle tenant namespaces and per-tenant event stores: the Chronicle
  multi-tenancy guidance.
- Building the React page that consumes identity: the Arc React guidance.
