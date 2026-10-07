<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authentication.md -->
# Authentication

Verified against `Cratis.Arc.Core` and `Cratis.Arc` `22.41.1`. Types are in
`Cratis.Arc.Authentication` and `Cratis.Arc.Identity`.

## The contract

```csharp
public interface IAuthenticationHandler
{
    Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context);
}
```

`IAuthentication` aggregates the discovered handlers: `HasHandlers`, and a
`HandleAuthentication` that returns the **first** result which is either
authenticated or carries a failure; otherwise `AuthenticationResult.Anonymous`.

```csharp
public record AuthenticationResult(
    ClaimsPrincipal? Principal = default,
    AuthenticationFailure? Failure = default)
{
    public static readonly AuthenticationResult Anonymous;
    public bool IsAuthenticated => Principal is not null;
    public static AuthenticationResult Failed(AuthenticationFailureReason reason);
    public static AuthenticationResult Succeeded(ClaimsPrincipal principal);
}
```

`AuthenticationFailure` is `record (AuthenticationFailureReason Reason)` and
`AuthenticationFailureReason` is a `ConceptAs<string>` with an implicit string
conversion.

There is **no** `IArcBuilder` extension and **no** options type for
authentication. Configuration is by discovery — implement the interface on a
public class and it is registered through the convention bindings.

## The Microsoft Identity Platform handler

`MicrosoftIdentityPlatformAuthenticationHandler` ships in **Arc.Core** and is
transport-agnostic. (Documentation that says it exists only in the ASP.NET Core
package and must be hand-written for Arc.Core is stale.)

It reads three headers, all constants on
`Cratis.Arc.Identity.MicrosoftIdentityPlatformHeaders`:

| Constant | Header |
| --- | --- |
| `PrincipalHeader` | `x-ms-client-principal` |
| `IdentityIdHeader` | `x-ms-client-principal-id` |
| `IdentityNameHeader` | `x-ms-client-principal-name` |

All three must be present or the result is `Anonymous`. The principal header is
base64-decoded and deserialized into `ClientPrincipal`:

```csharp
public class ClientPrincipal
{
    public string IdentityProvider { get; set; }
    public string UserId { get; set; }
    public string UserDetails { get; set; }
    public IEnumerable<string> UserRoles { get; set; }
    public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
}

public class ClientPrincipalClaim
{
    public string typ { get; set; }
    public string val { get; set; }
}
```

The lowercase `typ`/`val` members are the deliberate wire shape — do not
"correct" them.

A principal that fails to decode produces
`AuthenticationResult.Failed("Not authenticated - invalid representation of ClientPrincipal")`.

From a valid principal the handler builds a `ClaimsIdentity` whose
authentication type is `nameof(MicrosoftIdentityPlatformAuthenticationHandler)`:

1. every forwarded claim is copied as `Claim(typ, val)`;
2. `NameIdentifier`, `sub`, and any existing
   `urn:cratis:arc:identity:provider` claim are **removed** (the last one
   case-insensitively);
3. `Name` is set from `UserDetails`;
4. `NameIdentifier` and `sub` are set from the `x-ms-client-principal-id` header
   — not from the body;
5. `urn:cratis:arc:identity:provider` is written from `IdentityProvider` when it
   is not blank;
6. every entry of `UserRoles` becomes a `Role` claim.

## The reserved claim

`MicrosoftIdentityPlatformClaims.IdentityProvider` is
`"urn:cratis:arc:identity:provider"`. Removing any inbound claim of that type
before writing Arc's own buys **single provenance, not authenticity**: the claim
carries exactly one value, always from the same field of the forwarded principal.

Read it with `ClaimsPrincipal.FindFirst`/`FindAll` and never normalize the claim
type yourself. Those lookups compare the way the removal does; folding types with
`ToUpperInvariant` or `Trim` widens the match beyond what was removed, so a
forged type differing only by Unicode case folding or trailing whitespace would
match — and it precedes Arc's claim in the list.

The value's meaning is the ingress's choice, not Arc's: one ingress forwards a
canonical provider key, another an authentication scheme name or a display name
that changes when the provider is renamed. Treat it as metadata for telling
federations apart and for diagnostics. Where a durable provider key is needed,
read the claim the ingress publishes for that purpose.

Claim types outside the reserved set are copied through untouched.

## ASP.NET Core hosting

```csharp
services.AddMicrosoftIdentityPlatformIdentityAuthentication(scheme?);
```

registers the standard ASP.NET Core scheme backed by
`MicrosoftIDentityPlatformAuthHandler` (note the capital `D` in the type name),
whose `SchemeName` is `"MicrosoftIdentityPlatform"`. `builder.AddCratis()` (the
`Cratis` package) calls this for you and makes it the **default** authentication
scheme; `AddCratisArc()` from `Cratis.Arc` does not register it.

⚠️ Today the default is that a plain `builder.AddCratis()` host trusts the unsigned
`x-ms-client-principal*` headers from **any caller that can reach it** — it is safe
only behind the ingress described below (tracked as open Arc#2946). If the host is
not behind such an ingress, do not use this scheme: configure a credential-validating
ASP.NET Core scheme yourself.

## Forwarded headers, ingress requirements and protected introspection

The headers are only safe behind an ingress that **authenticates callers, strips
any caller-supplied identity headers, writes its own trusted replacements, and
prevents direct access to the backend**. The same applies to any custom header
such as `X-User-ID`. Adding a bearer validator does not make a separately accepted
forwarded-header mechanism safe.

Arc applies that rule to one place itself: the protected introspection catalogs
(`/.cratis/commands`, `/.cratis/queries`). By default those are enabled and
**anonymous**. Configure `Cratis:Arc:Introspection`:

| Option | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | `false` leaves both catalog routes unmapped |
| `RequireAuthentication` | `false` | `true` requires an authenticated caller, regardless of the host's default authorization policy |
| `Roles` | `null` | Comma-separated, any one grants access; needs `RequireAuthentication: true` and no empty entries |
| `TrustForwardedIdentityHeaders` | `false` | Accepts identity from the unsigned forwarded headers for the protected catalog; needs `Enabled` and `RequireAuthentication` |

Today the default is that the catalogs are public, and with
`RequireAuthentication: true` the forwarded headers are **ignored** for them until
you set `TrustForwardedIdentityHeaders: true`: the built-in
`MicrosoftIdentityPlatformAuthenticationHandler` returns
`AuthenticationResult.Anonymous` for a protected catalog endpoint, so the request
gets 401. **Only set the opt-in behind the trusted ingress above.**

- Startup fails closed for unsafe combinations: `Roles` or the trust option
  without the requirements above; on Arc.Core, `RequireAuthentication` with no
  handler, or with only the built-in header handler and no trust opt-in; on
  ASP.NET Core, a header-trusting scheme reachable from the default scheme or the
  catalog's policy schemes without the opt-in, and a reachable scheme with
  `ForwardDefaultSelector` when a header handler is registered.
- **Custom handlers.** Arc cannot tell whether your own `IAuthenticationHandler`
  reads forwarded headers, and startup accepts any custom handler as
  credential-validating. If yours builds an identity from ingress headers, read
  `IOptions<ArcOptions>` and return `AuthenticationResult.Anonymous` when
  `TrustForwardedIdentityHeaders` is `false` and
  `context.GetEndpointMetadata()?.RequireAuthentication == true`. A subclass of the
  ASP.NET Core header handler that overrides `HandleAuthenticateAsync` without
  calling the base is your own handler and carries the same obligation.
- These options do **not** change `/.cratis/identity-details/schema`, command and
  query invocation, or the user and tenant discovery endpoints. Restrict those at
  the ingress. On ASP.NET Core a protected catalog's 401/403 is the configured
  scheme's challenge, which a cookie scheme may turn into a redirect.

Discovery exposure as a whole is tracked in open Arc#2834.

## What is not here

There is **no** OpenID Connect, **no** JWT bearer, and **no**
`Microsoft.Identity.Web` anywhere in Arc — neither in source nor in the shipped
documentation. Validating a token inside the application is ordinary ASP.NET
Core authentication that you configure yourself; Arc's model is a trusted ingress
forwarding a principal.

## The security boundary, stated plainly

`x-ms-client-principal` is base64, not a signature, and Arc does not check who
sent it. Any caller that can reach the application can author the entire
serialized principal, including the fields Arc reads. Trust it exactly as far as
you trust that your ingress is the only thing that can set it — which means the
application must not be reachable any other way.
