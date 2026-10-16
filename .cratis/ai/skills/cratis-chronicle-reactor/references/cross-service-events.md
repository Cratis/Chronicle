<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/cross-service-events.md -->
# Cross-service events

This reference holds the outbox and inbox routing for cross-service facts.

### Cross-service events

Cross-service facts route through the outbox and inbox rather than a shared log.
The producer can append its public contract event to the outbox sequence or
publish it there as eligible projection/reducer event output; `[Public]` alone
does not forward every event-log append. The consuming observer listens on the
implicit inbox sequence for the source store.
Chronicle creates or reuses that subscription from the observer's `[EventStore]`
metadata, or from `[EventStore]` on the event type or its assembly. Put
`[assembly: EventStore("<source-store>")]` in a contracts project when every
event there originates from one service.

Requires Chronicle 19.62.1 for forwarded outbox events to retain append tags as
well as event-source/stream routing. Check the deployed kernel version; older
forwarded events stay untagged and are not backfilled by an upgrade. Cover a
matching and excluded inbox event before relying on tag-filtered observers.
