<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/public-event-coverage.md -->
# Public-event coverage (ESM v9)

Coverage for public events and routed producers.

Outbound publication needs `when append <private>` and `then <PublicEvent>`:
complete state, incomplete state, unchanged repeat, changed instance, separate
source ids and the exported field boundary. Reference publication and admitted
source-event captures execute; reached opaque event-target reducers remain typed
Unsupported. Read automations-and-translations `references/public-events-and-direction.md`
for the prerequisite and complete typed capture specifications.

For `source events`, use `given <FromEvent>` / `when append <FromEvent>`, not
`given capture` / `when capture` (PLAY0701). Cover first added, unchanged overlays,
updates after ordered givens, properties retained across different event types,
and independent keys. The key must be required on every source event, not a
context key (PLAY0700); optional foreign fixture `for` is not that key.
Assertions list all follow-up facts, never the input append itself. Mixed capture
and reaction expectations may need `then events in any order`; this is not proof
of observer order or redelivery deduplication. Children/nested over events and
context members other than `occurred` are not admitted execution.
For routed producers/observers, cover override versus inherited route, skipped
production route failure, matching/wrong source/stream and unrouted facts;
see toolchain `references/production-routes-and-filters.md`.
