<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/local-development.md -->
# Running with an identity locally

Source: `Cratis.Arc.Core` and `Cratis.Arc`.

## What Arc does not give you

There is **no** development identity provider, **no** fake or seeded principal,
**no** local sign-in page, and **no** development authentication handler anywhere
in Arc. `ArcOptions` has no switch that relaxes authentication. The only
"Development" type in the framework is `DevelopmentTenantIdResolver`, which is a
deprecated alias over the fixed tenant resolver and has nothing to do with
identity.

Guidance describing a `.cratis/.ai` local identity route is describing something
that does not exist.

## What Arc does give you

Two anonymous listing endpoints, both tagged for development in their metadata:

| Route | Returns |
| --- | --- |
| `GET /.cratis/users` | Every `User` from every registered `ICanProvideUsers` |
| `GET /.cratis/tenants` | Every `Tenant` from every registered `ICanProvideTenants` |

```csharp
public record User(ClientPrincipal MicrosoftIdentity, object Details);
public interface ICanProvideUsers { Task<IEnumerable<User>> Provide(); }
```

Both follow `Introspection.IdentityDiscovery` and discovery
access settings: anonymous by default only in Development, authenticated elsewhere,
or unmapped (404) if the host cannot authenticate. Return development fixtures,
not production users or tenants.
Source: [secure defaults](https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.mdx).

## Supplying an identity locally

Because the shipped handler reads forwarded headers, the way to run with an
identity locally is to send those headers yourself **after explicitly enabling**
`Cratis:Arc:TrustForwardedIdentityHeaders=true` in the isolated development host.
Without it they are ignored. All three are required:

- `x-ms-client-principal-id`
- `x-ms-client-principal-name`
- `x-ms-client-principal` — base64 of the `ClientPrincipal` JSON

```json
{
  "identityProvider": "<provider>",
  "userId": "<id>",
  "userDetails": "<display name>",
  "userRoles": ["<role>"],
  "claims": [{ "typ": "<claim type>", "val": "<value>" }]
}
```

Base64-encode that document and send it as `x-ms-client-principal`. `curl`
example shape:

```bash
curl -H "x-ms-client-principal-id: <id>" \
     -H "x-ms-client-principal-name: <name>" \
     -H "x-ms-client-principal: <base64>" \
     "https://<host>/.cratis/me"
```

`NameIdentifier` and `sub` come from the **id header**, not from `userId` in the
body, so the two must agree if anything downstream reads both.

## A browser cannot set headers on every transport

Server-Sent Events and WebSocket connections opened by the browser cannot carry
custom request headers. A local setup that authenticates purely by header will
therefore authenticate ordinary requests and not the observable-query streams.

Arc's own test applications solve this with a **cookie-based** development
handler carrying the same base64 `ClientPrincipal` under a cookie. That handler
lives in the test applications, not in any shipped package — if you need it, it
is an `IAuthenticationHandler` you write in your own application and register
only for development.

## Do not let development shortcuts become the deployment

Any local mechanism that lets the caller author its own principal is exactly the
production risk described in [authentication](authentication.md). Keep it behind
an environment check, and make sure the deployed application is unreachable
except through the ingress that owns those headers.

## Development fixtures

Those endpoints follow the discovery exposure settings above. Return development
fixtures from development providers; test deployed discovery access explicitly.
Forwarded headers sent locally require the host-wide trust opt-in too; do not
carry that shortcut into a directly reachable deployed host.
