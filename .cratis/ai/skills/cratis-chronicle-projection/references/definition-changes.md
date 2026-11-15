<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/definition-changes.md -->
# Definition changes replay

This reference holds how the kernel classifies a changed projection definition into no action, partial replay or full replay.

## Definition changes replay

Per `Documentation/hosting/configuration/observers.md`, the kernel
classifies a changed definition as no action, partial replay, or full replay.
`observers.definitionEvolution` defaults to `Automatic`; `PartialOnly` applies
partial/no-action plans but recommends full replay; `Manual` recommends both
replay kinds. Mapping, key, filter, join and child changes require full replay
when independence cannot be proved. Counters and joins are repaired by replay,
not by deploying corrected mappings: earlier double-counts remain until rebuilt.
A `FromAll` subscription change redefines existing projections.

Adding mappings for event types that already have history requires
**full** replay: `Automatic` rebuilds; `PartialOnly` and `Manual` recommend it.
The kernel no longer replays those mappings onto live state, where watermarks
could discard historical writes or counters could count twice. No history for
the added types means no action. Explicit partition replay is unchanged.
Verified in `Source/Kernel/Core/Projections/Projection.cs`
(`EvolveForAllNamespaces`).

Requires Chronicle 19.101.3 for explicit refusal of global projection replay.

Global projection replay (full or partition) is refused before writing. Do not promise to repair globally scoped state by replay.

Projection replay and catch-up each walk the sequence in **one globally ordered
job step**, not parallel per-source steps. A new failure during catch-up stops
that ordered step even if other sources are healthy. Full replay keeps the
observer inactive; completion routes to catch-up/live unless quarantined.
A next-event storage error leaves the cursor unchanged and waits for
retry; only `NotFound` proves absence. It does not certify previously skipped
history. Sources: `Documentation/projections/registration-lifecycle.md`,
`Documentation/concepts/observers.mdx`, `Observation/Jobs/{CatchUpObserver,ReplayObserver}.cs`
and `Observation/States/Routing.cs` (under `Source/Kernel/Core/`).

## Rolling deployment versions

Requires Chronicle 19.79.0 on client and kernel for projection registration's
`ChronicleOptions.SoftwareVersion`. It defaults to the entry assembly's informational version;
`Cratis:Chronicle:SoftwareVersion` overrides it, and startup warns on `1.0.0`.
Check `dotnet list package` and the kernel image tag; older kernels ignore it.

The kernel compares semantic-version precedence, including prerelease labels.
An older definition cannot overwrite a newer stored version, and an older
full-set registration cannot retire newer projections. Equal, missing or
unparseable versions retain ordinary registration behavior. An intentional
rollback must give the older code a software version higher than the stored
one. Changing only software version does not request a replay; definition
evolution policy still determines that.

## Namespace reconciliation

Requires Chronicle 19.81.1 for projections and captures to reconcile subscriptions
in every namespace, including runtime-created namespaces. Explicit ensure waits
for healthy subscriptions; implicit first-append creation is reconciled within
a watchdog interval. A broken definition does not block healthy ones or creation.
A lost subscription is repaired, but reconciliation never lifts quarantine,
revives a retired observer, or restarts a stopped/deleted capture. A kernel
restart or a newly added namespace still ends projection quarantine through a
fresh subscription; do not interpret ordinary watchdog repair as operator clear.

## Upgrade repair checks

Verified through **v19.110.1**:
`Source/Kernel/Storage.MongoDB/Sinks/Sink.cs`,
`Source/Kernel/Core/Projections/Engine/KeyResolvers.ParentHistory.cs` and the
tagged release notes:

Requires Chronicle 19.110.0 for MongoDB resumed bulk-window preservation, replaced-window fencing and historical parent-key resolution.

- MongoDB catch-ups resumed while clients restarted/re-registered could lose
  unflushed writes on older kernels. Upgrade, then replay affected
  projections, including catch-ups with `BulkWindowLost` or completed failures;
  silent loss may leave no log entry. The fixed sink preserves the window,
  refuses a window replaced before the first checkpoint and
  prevents queued writes crossing replay/catch-up collections.
- With content-keyed parents, children rebuilt before this fix may be under the
  wrong parent. Replay after upgrading; key lookup now uses the child's event
  position rather than the parent's latest key.
- These are kernel-backed storage/replay behaviors. In-process projection
  scenarios do not prove bulk durability or historical parent-key resolution.

Closing a stream does not erase its events or prevent a projection replay.
Keep the closing fact as history; see **cratis-chronicle-event-constraints**.
