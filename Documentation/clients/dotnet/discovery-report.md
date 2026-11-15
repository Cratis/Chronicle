---
title: Cratis Chronicle .NET discovery reports and identity drift
description: Inspect artifacts found by the Cratis Chronicle .NET client and check likely identity changes before registering them with the kernel.
---

Cratis Chronicle's .NET discovery report shows which artifacts the client found, while identity drift findings flag likely changes to their stored identities. Use `IEventStore.Discovery` when a type seems missing, and `Registration.IdentityDrift` when a rename may change how the kernel treats it. To check what actually registered, read the [registration outcome](registration-outcome.md).

## Inspect discovered artifacts

`Discovery` is an immutable snapshot of the last completed `DiscoverAll()` pass. Reading it does not start discovery. Before discovery runs, it equals `DiscoveryReport.NotRun`: `HasRun` is false and `Artifacts` is empty.

Given an `IEventStore` named `eventStore`, select the kind you want to inspect:

```csharp
using Cratis.Chronicle.Discovery;

var report = eventStore.Discovery;
var reactorCount = report.CountOf(ArtifactKind.Reactor);
var reactors = report.OfKind(ArtifactKind.Reactor);
```

`reactorCount` gives the number of reactors in the snapshot; `reactors` gives their entries. Each `DiscoveredArtifact` has a `Kind`, string `Id`, and nullable `ClrType`. The report covers `EventType`, `Constraint`, `Reactor`, `Reducer`, `Projection`, and `ReadModel`. A read model's id is its container name. Constraints and delegate reactors may have no CLR type. Failed projection builds have no discovered handler entry; look in `Registration.Artifacts` for their failures.

Explicit or runtime registrations added later leave the snapshot unchanged. The identity check builds a fresh view for each foreground registration run so it can include those additions. Background retries reuse findings without repeating snapshots or drift logs while the connection has not changed; reconnecting to a restarted kernel requires a fresh check. Webhooks, external subscriptions, and read model reactors are outside the report.

## Inspect likely identity changes

Before sending registrations, the client compares its artifacts with the kernel's stored event types and observers for the event store and namespace. Each finding records `Kind`, `PreviousId`, and `CurrentId`. For a manual registration run, read the findings afterwards:

```csharp
await eventStore.RegisterAll();
var findings = eventStore.Registration.IdentityDrift;
```

With automatic registration, read `Registration.IdentityDrift` after the normal connection-driven run instead of calling `RegisterAll()` again just to inspect it.

The check looks for an old observer of the same kind with a matching short name that is absent from the current declarations. Old reactors and reducers must no longer be subscribed. Projections skip that confirmation because the kernel keeps the old projection subscribed until registration retires it. For event types, the old candidate must be client-owned and uniquely match a nonempty coarse schema shape: a normalized view of the event's properties and their types. Once the current id is stored in the kernel, reconnecting does not report it again.

## Choose a policy

Set `ChronicleOptions.ArtifactIdentityDrift` to a value from `ArtifactIdentityDriftPolicy`:

| Policy | Behavior |
| --- | --- |
| `Warn` (default) | Log findings, include them in `Registration.IdentityDrift`, and continue registering. |
| `Fail` | Throw `ArtifactIdentityDriftDetected` before registering any artifacts when Warning-level reactor or event type findings exist. Information-level reducer and projection findings never fail registration. `Registration.Failure` records the failure and `IdentityDrift` retains all findings. Bounded and background retries do not retry this failure. |
| `Ignore` | Skip the identity check and its kernel calls. Discovery reporting still runs. |

Diagnostic calls share a five-second deadline. If the snapshot cannot be read, the client logs the skipped check and returns no findings, even with `Fail`. A failed subscription confirmation drops that reactor or reducer candidate.

Use `Fail` in CI or staging when you want detected reactor or event type changes to stop registration. The heuristic can miss renames, so review each finding: pin the previous reactor id to preserve its position, or deliberately remove an abandoned observer. See [Reactor identity and renaming](../../reactors/identity.mdx) and [the configuration reference](../../configuration/chronicle-options.mdx#artifactidentitydrift).

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
