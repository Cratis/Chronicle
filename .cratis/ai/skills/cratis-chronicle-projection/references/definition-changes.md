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

Closing a stream does not erase its events or prevent a projection replay.
Keep the closing fact as history; see **cratis-chronicle-event-constraints**.
