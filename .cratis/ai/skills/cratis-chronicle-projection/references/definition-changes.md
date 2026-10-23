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

Closing a stream does not erase its events or prevent a projection replay.
Keep the closing fact as history; see **cratis-chronicle-event-constraints**.
