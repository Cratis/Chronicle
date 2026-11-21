<!-- cratis-ai-managed: skills/cratis-arc-mcp/references/setup-and-profiles.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# MCP host setup, credential profiles and origins

## Contents

- Prerequisites
- Issuing a token
- Listeners and the capabilities port
- Credential profiles
- OAuth and Combined options
- Origin check and AllowedOrigins
- Development PAT adapters
- Curated tool sources

## Prerequisites

MCP serving needs all of these. Registration order does not matter; the check
runs when the host starts.

- Capability hosting: `WithCapabilities(...)`.
- A port for the capabilities endpoint group:
  `options.EndpointGroups.Assign("capabilities", port)`.
- `GatedEndpoints` is optional. Unset (the default), every endpoint on the
  capabilities port is gated after routing, including health and readiness
  probes; only the OAuth protected resource metadata route stays anonymous.
  Selective gating is an explicit opt-out: set
  `CapabilitiesOptions.GatedEndpoints` to a `CapabilityEndpointSelector` and
  `WithMcp` adds MCP to that selection, leaving other routes outside the gate.
  Missing capability hosting throws `McpRequiresCapabilities`.
- A verified-caller installation: `WithPersonalAccessTokens(PatOptions, callers => ...)`
  or `WithVerifiedCallers(...)`. Startup does not check that one is present;
  without it no caller is admitted.
- For PATs: `IPatAuthority`, `IPatRecoveryEpoch` and `IPatPolicy` must be
  registered, or issuance and verification are refused. Use
  `UseDevelopmentPersonalAccessTokenAdapters()` locally; production needs durable
  storage, product policy and recovery evidence.
- Arc has no issuance endpoint. Your endpoint calls
  `IPersonalAccessTokens.Issue(lifetime, scopes, cancellationToken)`. Maximum
  lifetime is 90 days; `Token.Reveal()` returns the token once. See
  "Issuing a token" below for what the call requires.
- Two installations exist (`VerifiedCallersBuilder`): PAT-only (person
  directory, no forwarding, no opt-in) and PAT plus signed browser issuance
  (`UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer(...)
  .WithTenantHeader(...).AddPublicKey(keyId, publicKeyPem)
  .UseProcessLocalNonceStore())` together with
  `AllowPersonalAccessTokenCallers()`, plus the forwarding-hop client
  certificate configuration). Mixing them throws
  `InvalidVerifiedCallerInstallation`. Do not also call `WithVerifiedCallers`;
  a second installation is rejected. Process-local nonce storage is a
  single-process development choice. Full example: the PAT adapter guide,
  section "Compose browser issuance and PAT callers".
- `PatOptions.AllowedScopes` must list every scope an MCP tool requires. The
  `PatOptions` resource key must be `direct`, `studio` or `stage-<app>`
  (`<app>`: lowercase letters, digits, hyphens, 1 to 48 characters, starting
  and ending with a letter or digit); `local` is invalid and throws
  `ArgumentException`. Startup logs warning event 5103 for a tool no PAT can
  satisfy; see the troubleshooting reference.

## Issuing a token

`Issue` succeeds only when all of these hold:

- The current caller is a verified `BrowserSession` or `OAuthGrant` caller
  with an owner (person, account key, tenant) for this resource. A PAT cannot
  issue another PAT.
- The caller carries valid assurance facts (the PAT module checks them).
- The lifetime is positive and within `PatOptions.MaximumLifetime` and 90 days.
- Every requested scope is in `PatOptions.AllowedScopes` **and** held by the
  issuing caller (a caller cannot mint a scope it does not hold).
- The three adapters are registered and the policy permits the record.

Otherwise the result is a refusal code such as `pat_issuance_refused`,
`pat_policy_refused`, `pat_commit_refused` or `pat_authority_unavailable`.

Limits to state to the user, not to work around:

- AuthProxy does not sign v2 forwarded identity yet; the v2 contract is a
  specification and the producer is not implemented. A real signed browser
  caller for issuance therefore has no released producer.
- The development adapters replace the three PAT ports only. They do not
  bootstrap issuance: you still need a verified browser or OAuth caller.
- Synthetic signing (`TestSigner` in `Samples/VerifiedCallerAcceptance.Specs`)
  is a spec-only test pattern. Never add a development signer, a debug
  endpoint or any issuance bypass to an application, and never ship signing
  keys that let a client mint callers.
- Without an issuance path, say so and stop; do not fabricate a token.

## Listeners and the capabilities port

Assigning the port does not bind it. Bind both the default public listener and a
listener on the capabilities port (Kestrel or `UseUrls`), even when the host
serves only `/mcp`. A request on the default port is not served. Map the
endpoint with `app.MapArcMcp()` after `app.UseCratisArc()`.

## Credential profiles

`McpOptions.Profile` selects the accepted credential kind.

| Profile | Accepts | Use for |
|---|---|---|
| `PatOnly` (default) | Personal access tokens | Command-line and IDE clients (Claude Code, Claude Desktop through `mcp-remote`, VS Code) |
| `OAuthOnly` | OAuth access tokens | Hosted connectors |
| `Combined` | Both, each by its own verifier | Hosted connectors while PATs keep working |

A caller verified with another credential kind (a browser session) is refused
with the same 401 as an unauthenticated request. Arc does not provide an OAuth
authorization server: it verifies grants through your installation and
publishes protected resource metadata; your own server issues tokens. Whether a
given connector can register with that server is outside Arc.

Client configuration for PAT clients: send `Authorization: Bearer <token>`.
Claude Code: `claude mcp add --transport http notes <url> --header "Authorization: Bearer $PAT"`.
VS Code: `.vscode/mcp.json` with `"type": "http"`, `url` and a `headers` entry
fed from a `promptString` input with `"password": true`. Never commit a token.

## OAuth and Combined options

```csharp
mcp.Configure(o =>
{
    o.ApplicationName = "notes";
    o.Profile = McpProfile.Combined;
    o.PublicOrigin = "https://notes.example.test";
    o.Resource = "https://notes.example.test/mcp";
    o.AuthorizationServers.Add("https://login.example.test");
});
```

- `PublicOrigin` is required: an origin (no path, query, fragment), `https`
  (`http` only for loopback).
- `Resource` must equal `PublicOrigin` plus `Path` (default `/mcp`).
- `AuthorizationServers` needs at least one absolute `https` URL (loopback may
  be `http`), no query, fragment or user info.
- Metadata is served at `/.well-known/oauth-protected-resource/mcp` and lists
  `resource`, `authorization_servers` and `scopes_supported` (the read scope and
  write scopes unless `ScopesSupported` is set).
- An unauthenticated request gets `WWW-Authenticate: Bearer resource_metadata="..."`.
  Under `PatOnly` the metadata URL answers 404 `not_found`.
- These values are never derived from the request host.

Invalid options throw `McpOptionsInvalid` naming the option and profile.

## Origin check and AllowedOrigins

Every request's `Origin` header is checked. No `Origin` (command-line and
desktop clients) is allowed. Allowed set: `PublicOrigin` plus `AllowedOrigins`.
Anything else is 403 `origin_not_allowed`, under every profile.

```csharp
mcp.Configure(o => o.AllowedOrigins.Add("http://localhost:6274")); // Inspector, test runs only
```

- Entries are bare origins: scheme, host, optional port; `https`, or `http` for
  loopback.
- Comparison ignores case. Several `Origin` values, or `null`, are always
  refused.
- Set the `Cratis.Arc.Mcp` log category to Debug: `McpAdmission` (event 5101)
  logs the refused origin and the reason, so copy the exact origin from there.
- The Inspector may connect through its own proxy process, which sends no
  `Origin`; then no entry is needed.
- Keep production `AllowedOrigins` empty unless a real browser client needs it.

## Development PAT adapters

`UseDevelopmentPersonalAccessTokenAdapters()`
(`Cratis.Arc.PersonalAccessTokens.Development`) registers process-local
`IPatAuthority`, `IPatPolicy` and `IPatRecoveryEpoch`. It follows
`WithPersonalAccessTokens` in any order and uses its limits.

- This extension method is the only public development PAT type. The
  authority, policy and recovery-epoch classes it registers are internal: do
  not reference or construct them.
- Credentials live in memory and disappear on restart.
- Host startup throws `InvalidOperationException` outside the Development
  environment, and each adapter also fails closed there.
- Never combine them with production adapters.
- They replace the adapters only; they do not bootstrap issuance. Issuing a
  token still needs a verified browser or OAuth caller (see "Issuing a
  token").

## Curated tool sources

For a tool that is not a plain command or query, implement `IMcpToolSource`
(`AddToolSource<T>()`), build descriptors with
`McpToolDescriptor.Custom(name, description, scopes, McpToolAccess.Read|Write)`.
A tool that writes must require `WriteScope` or one of `WriteScopes` in every
scope set it declares (`McpWriteToolLacksWriteScope`). `AlternativeScopes`
adds alternative scope sets. Read caller, tenant and scopes only from
`invocation.Caller`, never from the request.
