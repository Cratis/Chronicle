<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authproxy.md -->
# Cratis AuthProxy as the trusted ingress

Verified against Cratis AuthProxy `v2.30.5` (container image `cratis/authproxy`) and its documentation. AuthProxy is a reverse proxy that authenticates callers (OpenID Connect, JWT) and forwards the result to a backend in exactly the header contract Arc reads. It is the ingress the rest of this skill asks for. Other ingresses (Azure Container Apps or App Service EasyAuth, your own gateway) work if they meet the same four obligations; this page is specific to AuthProxy.

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

## Tenants and services

AuthProxy resolves the tenant through ordered strategies under `Cratis:AuthProxy:TenantResolutions` (`Host`, `Claim`, `Route`, `Specified`, `Default`, `SubHost`, `Selection`). The result reaches the backend as `x-cratis-tenant-id`, which is Arc's default tenant header, so an Arc application behind AuthProxy needs no tenancy configuration and the `UseHeaderTenancy("Tenant-ID")` workaround seen in older applications can be removed.

AuthProxy still forwards the legacy `Tenant-ID` and `Service-ID` alongside `x-cratis-tenant-id` and `x-cratis-microservice` during a transition. That forwarding is deprecated and will be removed in a future major release: read the `x-cratis-*` names. Tenant resolution says which tenant the request is for; it is not an authorization decision and does not isolate storage (see [tenancy](tenancy.md)).
