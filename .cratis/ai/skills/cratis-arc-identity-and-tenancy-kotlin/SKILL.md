---
name: cratis-arc-identity-and-tenancy-kotlin
description: Wire Kotlin or Java Arc authentication, application identity details and tenant resolution — AuthenticationHandler, IdentityDetailsProvider, tenancy.required, resolver precedence, ambient coroutine tenancy and the opt-in trusted platform-header bridge. Use for JVM Arc identity endpoints, tenant isolation or security ingress setup. Do not use for C# Arc identity, command input validation, or standalone Chronicle namespace resolution.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-identity-and-tenancy-kotlin/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Resolve identity and tenancy in JVM Arc

Authentication identifies the caller; authorization decides whether they may
execute an operation; tenant resolution selects their data boundary. Configure
and verify all three separately. A tenant header alone proves no membership.

## Verified product sources

Verified at `Arc.Kotlin v7.7.0`: `Source` authentication/identity/tenancy contracts,
`Integrations/SpringBoot/ArcTenancyProperties.java`, platform security classes,
`Documentation/guides/security.mdx`, `ambient-tenancy.mdx` and
`reference/configuration.mdx`. Use `io.cratis:arc-spring-boot-starter:7.7.0`
(or `io.cratis:cratis:7.7.0` for Chronicle integration), with matching plugin/KSP.
These are JVM contracts; do not infer the same switches exist in Arc .NET.

## Authenticate and expose identity details

Register ordered `AuthenticationHandler` beans. A handler gets immutable
`AuthenticationRequestContext`: case-insensitive headers, cookies, captured
principal and selected tenant. Return:

- `AuthenticationResult.ANONYMOUS` when credentials are not recognized, so the
  next handler can try.
- `AuthenticationResult.failed(...)` for recognized but rejected credentials.
- `AuthenticationResult.succeeded(...)` with an `ArcPrincipal` on success.

Success and failure are terminal; a later handler cannot override a rejection.
Spring interleaves Kotlin handlers and Java `AsyncAuthenticationHandler` beans
in global `@Order`. Java handlers return `CompletionStage`, which Arc adapts.
Do not block while verifying credentials. Public authentication errors are
generic 401 responses, not the handler's private reason.

With Spring Security present, Arc captures its principal before suspending
work. `ROLE_` authorities become unprefixed Arc roles. Without it, Arc captures
the servlet principal. Never read thread-local security state after suspension.

Register exactly one `IdentityDetailsProvider<T>` to serve `GET /.cratis/me`.
Supply `detailsType: Class<T>` and return `IdentityDetails<T>` from
`provide(IdentityProviderContext)`, including `isUserAuthorized` and the
application payload. Use a concrete DTO KSP can describe; an unsupported
identity shape fails `ARCKSP0307`. Java uses `AsyncIdentityDetailsProvider<T>`.
Multiple providers fail startup. The client-readable identity cache cookie is
excluded from authentication input and cannot authenticate a later request.

`@Authorize`, `@Roles`, `@AllowAnonymous` declare authorization. An operation's
own metadata replaces its class default wholesale; contradictions on the same
declaration fail `ARCKSP0108`. See `cratis-arc-command-kotlin` for precedence.

## Configure inbound tenant resolution

Keys below are under `cratis.arc`:

| Key | Default / contract |
| --- | --- |
| `tenancy.resolvers` | `header`; ordered `fixed`, `header`, `query`, `claim`, `subdomain`, `development` |
| `tenancy.required` | `false`; true rejects unresolved command, query, observable and identity requests with 400 |
| `tenancy.header-name` | unset uses `tenant-header` (default `x-cratis-tenant-id`); case-insensitive |
| `tenancy.query-parameter-name` | `tenantId`; case-sensitive |
| `tenancy.claim-type` | `tenant_id` |
| `tenancy.base-domain` | empty; subdomain resolution needs a valid multi-label DNS base domain |
| `tenancy.fixed-tenant-id` | `development` |
| `tenancy.constrain-to-authenticated-claims` | `true`; when membership claims exist, selection must match one |

Keep claim constraints on. Absence of membership claims is not proof the caller
belongs to every tenant; add the application authorization policy required by
your domain. Do not expose development/fixed selection as production isolation.
`TenantsProvider` lists development tenants for tools; it does not resolve a request.

Resolution is not storage routing. Plain repository injection is ordinary
Spring DI, not automatic tenant isolation. Use certified tenant-aware store
resolvers and test two-tenant isolation; `cratis-arc-spring-data-kotlin` covers
the storage boundary. Chronicle namespace mapping is a separate integration concern.

## Carry an already resolved tenant

`withTenant(TenantId(...))` installs a `TenantCoroutineContext` that survives
suspension and dispatcher switches. `currentTenant()` returns the nearest
tenant or `null`; `currentTenantOrNotSet()` returns `TenantId.NOT_SET` when absent.
Nested scopes shadow the outer tenant for their duration. These functions do
not resolve HTTP requests or authorize a tenant.

Java on a plain thread uses `TenantContextBridge.withTenant` and
`TenantContextBridge.currentTenant()`. The bridge blocks through `runBlocking`;
do not call it inside an active coroutine. Kotlin coroutine code uses the
suspending APIs, not a `ThreadLocal`. Establish scopes explicitly for background
work; do not assume an inbound request or command establishes every ambient seam.

## Opt into trusted platform headers only behind secure ingress

`cratis.arc.platform-identity.enabled` defaults to `false`. Base64 is unsigned
input, not authentication. Enable the bridge only behind authenticated ingress
that strips caller-supplied `x-ms-client-principal`,
`x-ms-client-principal-id`, `x-ms-client-principal-name` and rewrites them from
verified identity. Block direct backend access.

Supply an `ArcPlatformIdentityTrust` bean checking the actual deployment
boundary on every submission; the default denies all. Do not trust forwarded
headers or an address a request wrapper rewrote without verified proxy handling.
Header parsing cannot install firewall, TLS or ingress guarantees.

With Spring Security absent or the bridge disabled, Arc registers no platform
filter/chain. With no application chain, enabling it creates a conservative
chain: all routes need authentication and CSRF remains enabled. Arc does not
rewrite an application `SecurityFilterChain`; add `ArcPlatformAuthenticationFilter`
after existing authentication and before `AnonymousAuthenticationFilter` only
in chains intended to accept platform identity. Keep servlet registration
disabled to prevent double execution outside Spring Security.

Valid submissions never replace an already authenticated non-anonymous identity.
Invalid/partial/repeated submissions fail generically with 401 (earlier filters
may reject first). The bridge retains identity in request attributes, not the
HTTP session. Never log identity headers, raw claims or the cache cookie.
Consult the tagged security guide for the bounded header schema and limits
before changing an ingress adapter; do not hand-roll permissive parsing.

## Verify and route

- Test the resolver order, missing tenant, membership mismatch and valid selection
  with `withTenantResolution` in `cratis-arc-testing-kotlin`.
- Exercise anonymous, accepted and rejected credentials plus identity-details
  authorization; do not mistake a successful details response for authentication.
- Verify tenant propagation across a dispatcher switch and two isolated stores.
- If enabling platform identity, test spoofed/direct submissions fail, legitimate
  ingress succeeds, CSRF remains enforced, and the filter runs once.
- Run the application's `./gradlew build` and real storage/ingress checks.

Command rules: `cratis-arc-validation-kotlin`. Standalone Chronicle namespaces:
`cratis-chronicle-client-kotlin`, not this resolver configuration.
