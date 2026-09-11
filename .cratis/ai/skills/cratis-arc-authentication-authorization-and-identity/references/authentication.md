<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authentication.md -->
# Authentication

Verified against `Cratis.Arc.Core` and `Cratis.Arc` `22.10.4`. Types are in
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
whose `SchemeName` is `"MicrosoftIdentityPlatform"`. `builder.AddCratis()` calls
this for you.

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
