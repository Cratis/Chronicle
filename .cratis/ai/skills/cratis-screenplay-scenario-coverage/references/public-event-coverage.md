<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/public-event-coverage.md -->
# Public-event coverage (ESM v9)

Coverage for public events and routed producers.

Outbound publication needs `when append <private>` and `then <PublicEvent>`:
complete state, incomplete state, unchanged repeat, changed instance, separate
source ids and the exported field boundary. Reference publication is executable;
source-event captures and reached opaque event-target reducers are typed
Unsupported. Read automations-and-translations `references/public-events-and-direction.md`.
For routed producers/observers, cover override versus inherited route, skipped
production route failure, matching/wrong source/stream and unrouted facts;
see toolchain `references/production-routes-and-filters.md`.
