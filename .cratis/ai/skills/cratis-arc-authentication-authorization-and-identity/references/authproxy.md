<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authproxy.md -->
# Cratis AuthProxy as the trusted ingress

Source: Cratis AuthProxy (container image `cratis/authproxy`) and its documentation. AuthProxy is a reverse proxy that authenticates callers (OpenID Connect, JWT) and forwards the result to a backend in exactly the header contract Arc reads. It is the ingress the rest of this skill asks for. Other ingresses (Azure Container Apps or App Service EasyAuth, your own gateway) work if they meet the same four obligations; this page is specific to AuthProxy.

## What it does to the headers

Before forwarding any request, AuthProxy removes every inbound header whose name begins `x-ms-client-principal` (whatever the suffix) and both tenant headers, `x-cratis-tenant-id` and the legacy `Tenant-ID`. For an authenticated caller it then writes its own:

| Header | Carries |
| --- | --- |
| `x-ms-client-principal` | The client principal, base64 JSON |
| `x-ms-client-principal-id` | The provider-local subject |
| `x-ms-client-principal-name` | The display name; `x-ms-client-principal-name*` holds the RFC 8187 form when the plain value cannot travel verbatim |
| `x-cratis-tenant-id` | The tenant AuthProxy resolved (absent when none resolved) |

An unauthenticated caller cannot smuggle identity headers downstream, and a client cannot choose the tenant a backend sees. AuthProxy also calls the backend's `GET /.cratis/me` with the same headers to enrich the identity, so a backend behind it must serve that endpoint (Arc does by default).

## The one decision that cannot be fixed afterwards

The headers are **not signed**. A backend cannot tell a request that came through AuthProxy from one that did not, except by the network it arrived on. Only AuthProxy may be able to open a connection to the backend (private network, internal ingress, network policy); otherwise anyone can impersonate any user by sending the header, and everything still appears to work. This is the same trust boundary this skill describes for any ingress. Platform isolation recipes are in the AuthProxy documentation (`Documentation/azure`, `Documentation/aspire`).

## `/.cratis/me` is an authorization decision

`IdentityVerification` on an AuthProxy service defaults to `Required` (fail closed). Only a `2xx` with `"isAuthorized": true` admits the caller. A `404`, an unreachable backend, a timeout (default ten seconds), any other non-`2xx`, an empty or unparseable body, a missing verdict, or `isAuthorized: false` all deny. The earlier `BestEffort` mode denied only on `403`. Consequently your `IProvideIdentityDetails` result is the admission verdict at the proxy, not only display data: return `IsUserAuthorized = false` deliberately, and keep the endpoint reachable and fast. A service can opt back into `BestEffort` explicitly; do not do that to hide a broken identity endpoint.

## Multi-host sessions are opt-in

Requires AuthProxy 2.32.4 for the multi-host/crawler/invitation contract below; check the deployed version, or retain host-only sessions and the older error-page contract.

Defaults stay host-only with cookie name
`.Cratis.AuthProxy.Auth.v2`, per-host handshakes and relative return URLs.
`Session:CookieDomain` shares sessions beneath a parent domain;
`Session:CookieName` isolates independent deployments. `SignIn:CallbackHost`
centralizes OAuth/OIDC handshakes and requires the cookie domain. Restrict
`AllowedHosts`; never broaden hosts merely to make shared cookies work.

The callback and declared service hosts must lie inside the cookie domain;
`SubHost` tenant resolution cannot be combined with it. Absolute returns require
the callback host and an allowed origin inside that domain on the request's
scheme; otherwise they reduce to a local path. Invitations, credential links and
registration still start on their original host and need provider callbacks
there. Use the product's one-proxy/several-hosts migration guide, not an inferred
cross-domain cookie recipe.

`AuthProxy:SearchIndexing:Exclude` optionally serves anonymous
`robots.txt` and adds `X-Robots-Tag: noindex, nofollow`. It is off by default and
is a crawler policy, not authorization. Invitation error-page tokens
`{{invitedEmail}}` and `{{assertedEmail}}` are blank for addresses outside ASCII
letters, digits and `._+-@`; do not assume these placeholders always contain text.
Signed-in `/register` forwarding needs no host workaround.

## Starting AuthProxy from a Screenplay application's contract

Requires AuthProxy 2.40.0 or later for `Cratis:AuthProxy:Application`; an older image ignores it and starts with no providers.

AuthProxy can start from two inputs: the **provider list** of a Screenplay model (kind and identity per provider) and the **values of a Cratis environment**. It translates them onto its ordinary `Cratis:AuthProxy` options, so an application behind it trusts the proxy exactly as described above. The model never carries provider configuration; see `cratis-screenplay-authentication-and-onboarding` for the keys and `cratis-environments-and-secrets` for the values.

```json
{
  "Cratis": { "AuthProxy": { "Application": {
    "EnvironmentName": "production",
    "Providers": [
      { "Kind": "EntraId", "Identity": "EntraId" },
      { "Kind": "OpenId", "Identity": "Partner" }
    ]
  } } }
}
```

- A provider's `Kind` is `EntraId`, `GitHub`, `Google`, `Apple` or `OpenId`; `Identity` is the model's alias, otherwise the provider name, unique ignoring case. The identity is also the scheme (lowercase, spaces to hyphens), so the callback is `/signin-<scheme>`.
- Required keys per kind: `clientId` and `clientSecret` for all; plus `tenantId` for `EntraId` and `authority` for `OpenId`. Each is addressed as `authentication.providers.<Identity>.<key>`.
- The contract cannot be combined with providers configured directly under `Authentication:OidcProviders` or `OAuthProviders`; AuthProxy refuses to start.
- A missing required key stops AuthProxy before it listens (exit code 78), naming the key, provider and environment, never a value.
- **Secrets are never read from arguments and never logged.** A key resolves from `Cratis:AuthProxy:Application:Environment:<key>`, then the same key with `_FILE` appended (a path to a file holding the value), then a file named exactly `<key>` in `SecretsDirectory`. Mount secrets as files and point `SecretsDirectory` at the folder; do not put a secret in an environment variable or an image.
- `Application:Development=true` allows HTTP only on loopback addresses; every other URL must be HTTPS. Never set it outside local development.
- With `Application:Onboarding=true`, supply the `onboarding.*` keys for the application's own Ante and every provider also gets invitation completion; see `cratis-onboarding-runtime`.

## Tenants and services

AuthProxy resolves the tenant through ordered strategies under `Cratis:AuthProxy:TenantResolutions` (`Host`, `Claim`, `Route`, `Specified`, `Default`, `SubHost`, `Selection`). The result reaches the backend as `x-cratis-tenant-id`, which is Arc's default tenant header, so an Arc application behind AuthProxy needs no tenancy configuration and the `UseHeaderTenancy("Tenant-ID")` workaround seen in older applications can be removed.

AuthProxy still forwards the legacy `Tenant-ID` and `Service-ID` alongside `x-cratis-tenant-id` and `x-cratis-microservice` during a transition. That forwarding is deprecated and will be removed in a future major release: read the `x-cratis-*` names. Tenant resolution says which tenant the request is for; it is not an authorization decision and does not isolate storage (see [tenancy](tenancy.md)).
