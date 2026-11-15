<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/references/webhooks.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Webhook delivery policy

## Contents

- Validate the destination, not just the registration
- Independent delivery retries
- OAuth scope and token refresh
- Managed identity
- Quarantine probes are real deliveries

Verified at Chronicle **v19.110.1**: `Documentation/webhooks/index.mdx`,
`Source/Kernel/Core/Configuration/{Webhooks,WebhookManagedIdentity,WebhookProbe}.cs`,
`Source/Kernel/Core/Observation/Webhooks/` (including `Validation/`) and
`Source/Kernel/Server/Observation/Webhooks/ManagedIdentityCredentialFactory.cs`.
Configuration below sits under `Cratis:Chronicle:Webhooks`.

## Validate the destination, not just the registration

Requires Chronicle 19.100.0 for webhook target validation.

`Validation` is `Warn` (default) or `Enforce`. Warn logs violations without
blocking, except unapproved cloud metadata addresses. Do not mistake the default
for full SSRF protection or approve metadata hosts in normal deployments.
Enforce requires HTTPS and approved hosts, checking private/special resolved
addresses at connect time too. Redirects are not followed. OAuth authorities use the same policy.
`AllowInsecureHttp` applies only in Development.

`AllowedHosts` accepts exact hosts and wildcard subdomains; an event store's
`EventStoreAllowedHosts` entry **replaces** the global list. An exact host entry
can intentionally admit that host's private addresses. Bound exceptions to
trusted receivers; wildcards approve subdomains, not their parent domain.
Never widen the list just to silence a refusal.

Requires Chronicle 19.106.0 for validation of stored active definitions.

Existing active definitions are checked on activation/restart.
Enforce quarantines a disallowed target without deleting/deactivating the
stored definition; Warn logs once per activation and continues. Fix the target
or approved policy, then clear quarantine: clearing revalidates, so it does not
bypass a bad target. A connect-time policy failure is a configuration problem,
not a transient failure retried for 24 hours.

## Independent delivery retries

Requires Chronicle 19.104.0 for webhook-specific retry policy.

Each delivery makes one HTTP attempt. `Retry` is independent of global observer
settings: delays are 1 s, 5 s, 30 s, 2 min, 10 min, 30 min, then hourly, with
±20% jitter. The budget is 24 hours, then the partition is parked. Five parked
partitions or at least half the failing partitions parked quarantines the
observer. Override the schedule, maximum delay, attempts, budget and quarantine
thresholds only with a bounded recovery policy. `RetryDelaySeconds` is retained
but has no effect.

Retry deadlines/counts/positions survive restart; manual partition retry overrides
a pending deadline. A per-target (scheme/host/port) circuit defers deliveries
without consuming attempts. `429`/`503` with valid `Retry-After` defer without
consuming attempts, capped by `MaximumRetryAfterSeconds` (default 24 hours).
`410` parks immediately. Other `4xx` except `408` and `429` quarantine as
configuration failures. Catch-up/replay respect the pending deadline.

## OAuth scope and token refresh

Requires Chronicle 19.98.0 for OAuth scope, token reuse and one-shot 401 refresh.

The .NET webhook builder accepts
`WithOAuth(authority, clientId, clientSecret, scope)`; scope is optional, and
re-registering without it clears a previously configured scope. Entra client
credentials need the receiver's `api://<application-id>/.default` scope. Tokens
are shared by token endpoint/client/scope/credentials and refreshed before
expiry; tokens lacking expiry are not cached. A 401 refreshes and retries the
same request once; other responses do not trigger an OAuth retry. This is
separate from the observer retry policy. Never put credentials in examples or
logs. Verified in `Source/Clients/DotNET/Webhooks/IWebhookDefinitionBuilder.cs` and
the tagged webhook documentation.

## Managed identity

Requires Chronicle kernel 19.110.0 and .NET packages 19.110.1 for managed identity webhooks.

System-assigned/user-assigned Azure managed identity and AKS workload identity
can authorize a webhook without storing a client secret. The operator must
allow exact scopes in `ManagedIdentity.AllowedScopes`; the empty default
disables this. Non-empty client ids must be in `AllowedClientIds`; an empty id
needs `AllowSystemAssigned=true`. The receiver must explicitly match the host
allow-list **even in Warn mode**; delivery rechecks approval before acquiring a
token. This lends the **kernel's** identity, not the registering client's. Its
principal still needs the receiver's application role. Register with the .NET
builder's `WithManagedIdentity(scope, clientId)` (client id optional). Tokens are
memory-only and refreshed before expiry. A 401 discards the cached token and
its credential instance, then retries once; Azure's own cache means a different
token is not guaranteed. Never log tokens. Verified in
`Documentation/hosting/azure.md` and `Source/Clients/DotNET/Webhooks/IWebhookDefinitionBuilder.cs`.

## Quarantine probes are real deliveries

Requires Chronicle 19.110.0 for automatic webhook quarantine probes.

`Probe.Enabled` defaults to true. Retry-budget or delivery/configuration
quarantines can probe with exponential jittered backoff (initial 60 s, maximum
1 hour, timeout 30 s); manual quarantines and parked partitions are not
probed. A probe sends the **first pending event** without clearing quarantine
first. Success records it as handled, clears quarantine and resumes in order.
Failure leaves quarantine intact. Probes revalidate targets and consume neither
normal retry budget nor circuit samples. Set `Probe.Enabled=false` when
quarantined receivers must get no automatic traffic. Receivers must remain
idempotent; a probe is not a read-only health check.
