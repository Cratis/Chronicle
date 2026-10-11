---
title: Discovery report and identity drift
description: Inspect discovered .NET client artifacts and configure diagnostics for likely identity changes before registration.
---

Use `IEventStore.Discovery` to inspect what the .NET client found, and `Registration.IdentityDrift` to inspect likely renames before registration. These are diagnostics, not proof that every artifact registered successfully; use the [registration outcome](registration-outcome.md) for that.

## Inspect discovered artifacts

`Discovery` is an immutable snapshot of the last completed `DiscoverAll()` pass. Reading it does not start discovery. Before discovery runs, it equals `DiscoveryReport.NotRun`: `HasRun` is false and `Artifacts` is empty.

Each `DiscoveredArtifact` has a `Kind`, string `Id`, and nullable `ClrType`. The kinds are `EventType`, `Constraint`, `Reactor`, `Reducer`, `Projection`, and `ReadModel`. Read model ids are container names. Constraints and delegate reactors may have no CLR type. Failed projection builds do not appear as discovered handlers; their failures belong to `Registration.Artifacts`.

Given an `IEventStore` named `eventStore`:

```csharp
using Cratis.Chronicle.Discovery;

var report = eventStore.Discovery;
var reactorCount = report.CountOf(ArtifactKind.Reactor);
var reactors = report.OfKind(ArtifactKind.Reactor);
```

Explicit or runtime registrations added later do not mutate this snapshot. The identity check builds a fresh view for each foreground registration run, including those additions. Background retries reuse that run's findings without repeating snapshots or drift logs. Webhooks, external subscriptions, and read model reactors are outside the report.

## Inspect likely identity changes

Before registering, the client compares its artifacts with the kernel's stored event types and observers for the event store and namespace. Each finding records `Kind`, `PreviousId`, and `CurrentId`:

```csharp
await eventStore.RegisterAll();
var findings = eventStore.Registration.IdentityDrift;
```

An observer candidate must have the same kind and a matching short name, and be absent from the current declarations of that kind. Reactors and reducers must no longer be subscribed. Projections skip subscription confirmation: the kernel keeps the old projection subscribed until registration retires it. An event type candidate must be client-owned and uniquely match a nonempty coarse schema shape. A current id already stored in the kernel is not reported again on reconnect.

## Choose a policy

Set `ChronicleOptions.ArtifactIdentityDrift` to a value from `ArtifactIdentityDriftPolicy`:

| Policy | Behavior |
| --- | --- |
| `Warn` (default) | Log findings, include them in `Registration.IdentityDrift`, and continue registering. |
| `Fail` | Throw `ArtifactIdentityDriftDetected` before registering any artifacts when Warning-level reactor or event type findings exist. Information-level reducer and projection findings never fail registration. `Registration.Failure` records the failure and `IdentityDrift` retains all findings. Bounded and background retries do not retry this failure. |
| `Ignore` | Skip the identity check and its kernel calls. Discovery reporting still runs. |

Snapshot failures never block registration, even with `Fail`. The client logs the skipped check and returns no findings. A failed subscription confirmation drops that reactor or reducer candidate. Diagnostic calls share a five-second deadline.

`Fail` is useful in CI or staging, not a guarantee that every rename will be caught. Review each finding before acting: pin the previous reactor id to preserve its position, or deliberately remove an abandoned observer. See [Reactor identity and renaming](../../reactors/identity.mdx) and [the configuration reference](../../configuration/chronicle-options.mdx#artifactidentitydrift).

## Log levels

| Level | Message |
| --- | --- |
| Information | Discovery totals per kind, event store, and namespace. |
| Debug | Discovered ids per kind. |
| Warning | Likely reactor rename: catch-up re-runs side effects; pin the old id or remove the orphan. |
| Information | Likely reducer or projection rename: the read model rebuilds; the kernel also retires renamed projections. |
| Warning | Likely event type rename: stored history remains under the old id. |
| Warning | Snapshot unavailable: the check is skipped and registration continues. |
| Debug | Subscription confirmation or report building failed: the diagnostic is omitted. |

## Limits of the heuristic

- During a rolling deployment, a still-subscribed old reactor or reducer is filtered out. Once the new id is registered, later checks stay quiet, so this can miss a rename.
- Two services sharing a store and a short type name can look like a rename when one is down. Projections can match even while subscribed, so same-named projections from separate services are more prone to false positives. Reducer and projection findings remain informational under every policy. Review the evidence; there is no per-id allow-list.
- Same-shaped events can match coincidentally. Empty and ambiguous shapes are excluded, but a rename accompanied by schema changes can be missed.
- Event types cannot be removed to acknowledge a finding under `Fail`. Preserve the old id when evolving its contract, or switch to `Warn` after reviewing an intentional new identity. See [Event type migrations](../../concepts/event-type-migrations.mdx).
