---
title: Telemetry names
description: Migrate Chronicle metrics, attributes, and client spans to the shared Cratis OpenTelemetry naming convention.
---

Chronicle publishes kernel metrics under `cratis.chronicle.*`, with UCUM units and shared attribute keys. The public .NET registry is `Cratis.Chronicle.Diagnostics.WellKnownTelemetryNames`. Client metrics will use `cratis.chronicle.client.*`; this change does not add client metrics.

## Compatibility and migration

Legacy metric names and their original units, descriptions, and attribute keys remain published alongside the convention names for one minor release. Do not sum both series: they represent the same measurements. Switch dashboard queries and alerts to the new names and dimensions together. The legacy series and legacy span keys are deprecated for removal in the next major version.

The retry-attempt counter is an exception: `chronicle-observer-partition-retry-attempts` remains at its original recording point, after delivery fencing succeeds. It has no convention counterpart. Use `cratis.chronicle.observer.partition_failures` to count failed handling attempts. If recording a fence throws, a failure is counted but a retry attempt is not.

Metric names below are raw OpenTelemetry names. Prometheus exporters typically replace dots with underscores and append `_total` to counters. Never include `_total` in the instrument name. Exporter translation settings can affect the final query spelling.

## Kernel metrics

Every convention suffix in this table has the prefix `cratis.chronicle.`. Legacy names are complete. Brace units count the named entity; `s` is seconds and `1` is dimensionless.

| Convention suffix | Unit | Legacy instrument |
| --- | --- | --- |
| `observer.successful_observations` | `{observation}` | `chronicle-observer-successful-observations` |
| `observer.partition_failures` | `{failure}` | `chronicle-observer-partitions-failed` |
| `observer.partitions_quarantined` | `{partition}` | `chronicle-observer-partitions-quarantined` |
| `observer.partition_quarantines_cleared` | `{partition}` | `chronicle-observer-partition-quarantines-cleared` |
| `observer.quarantines` | `{quarantine}` | `chronicle-observer-quarantined` |
| `observer.suspected_skipped_events` | `{event}` | `chronicle-observer-events-suspected-skipped` |
| `observer.zero_work_catch_ups` | `{catch_up}` | `chronicle-observer-zero-work-catch-ups` |
| `observer.claim_divergences` | `{divergence}` | `chronicle-observer-claim-divergences` |
| `webhook.probes_scheduled` | `{probe}` | `chronicle-webhook-probe-scheduled` |
| `webhook.probe_attempts` | `{probe}` | `chronicle-webhook-probe-attempts` |
| `webhook.probe_failures` | `{probe}` | `chronicle-webhook-probe-failures` |
| `webhook.probe_successes` | `{probe}` | `chronicle-webhook-probe-successes` |
| `webhook.probe_resumes` | `{probe}` | `chronicle-webhook-probe-resumes` |
| `webhook.probes_deferred` | `{probe}` | `chronicle-webhook-probe-deferred` |
| `webhook.probes_skipped` | `{probe}` | `chronicle-webhook-probe-skipped` |
| `webhook.probes_cancelled` | `{probe}` | `chronicle-webhook-probe-cancelled` |
| `webhook.circuit_openings` | `{opening}` | `chronicle-webhook-circuit-opened` |
| `webhook.circuit_rejections` | `{delivery}` | `chronicle-webhook-circuit-rejected` |
| `event_sequence.appended_events` | `{event}` | `chronicle-event-sequences-appended-events` |
| `event_sequence.duplicate_sequence_numbers` | `{event}` | `chronicle-event-sequences-duplicate-event-sequence-numbers` |
| `event_sequence.append_failures` | `{event}` | `chronicle-event-sequences-failed-appended-events` |
| `event_sequence.constraint_violations` | `{violation}` | `chronicle-event-sequences-constraint-violations` |
| `event_sequence.unknown_append_outcomes` | `{append}` | `chronicle-event-sequences-append-outcomes-unknown` |
| `constraint_index.update_failures` | `{failure}` | `chronicle-event-sequences-constraint-index-update-failures` |
| `constraint_index.settled_errors` | `{event}` | `chronicle-event-sequences-constraint-index-settled-errors` |
| `constraint_index.poison_events` | `{event}` | `chronicle-event-sequences-constraint-index-poison-events` |
| `constraint_index.repairs` | `{repair}` | `chronicle-event-sequences-constraint-index-repairs` |
| `constraint_index.refusals` | `{append}` | `chronicle-event-sequences-constraint-index-refusals` |
| `constraint_index.lag` | `{event}` | `chronicle-event-sequences-constraint-index-lag` |
| `appended_events_queue.enqueued_events` | `{event}` | `chronicle-appended-events-queue-events-enqueued` |
| `appended_events_queue.handled_events` | `{event}` | `chronicle-appended-events-queue-events-handled` |
| `appended_events_queue.handling_failures` | `{failure}` | `chronicle-appended-events-queue-events-handling-failures` |
| `appended_events_queue.spilled_events` | `{event}` | `chronicle-appended-events-queue-events-spilled-to-catchup` |
| `appended_events_queue.catch_up_trigger_budget_exceeded` | `{trigger}` | `chronicle-appended-events-queue-catchup-trigger-budget-exceeded` |
| `appended_events_queue.suspected_undelivered_events` | `{event}` | `chronicle-appended-events-queue-suspected-undelivered-events` |
| `appended_events_queue.observer_spills` | `{spill}` | `chronicle-appended-events-queue-observers-spilled-to-catchup` |
| `alert.failed_transitions` | `{transition}` | `chronicle-alert-transitions-failed` |
| `alert.open_incidents` | `{incident}` | `chronicle-alerts-open-incidents` |
| `alert.open_incidents_available` | `1` | `chronicle-alerts-open-incidents-available` |
| `abandoned_delivery.late_results` | `{result}` | `abandoned-delivery-late-results` |
| `abandoned_delivery.hold_expirations` | `{delivery}` | `abandoned-delivery-hold-expirations` |
| `abandoned_delivery.fence_duration` | `s` | `abandoned-delivery-fence-duration` |
| `connected_client.count` | `{client}` | `chronicle-connected-clients` |
| `sql.schema_lock.wait_duration` | `s` | `chronicle-sql-schema-lock-wait` |
| `sql.live_query.poll_failures` | `{poll}` | `chronicle-sql-live-query-poll-failures` |
| `sql.upsert.recovered_collisions` | `{collision}` | `chronicle-sql-upsert-collisions-recovered` |
| `sql.composite_key.stamp_failures` | `{failure}` | `chronicle.sql.composite_key.stamp_failures` |
| `sql.composite_key.collisions` | `{collision}` | `chronicle.sql.composite_key.collisions` |

The meter is `Cratis.Chronicle`, except for the two composite-key instruments, which use `Cratis.Chronicle.Storage.Sql.CompositeKeyShadow`. Scope names do not change.

## Metric attributes

The convention series use only the shared keys below, not both sets of keys on each measurement. Legacy series retain the legacy keys. Event source ids and partition ids remain excluded from metric dimensions in every identifier exposure mode.

| Legacy key | Shared key |
| --- | --- |
| `EventStore` | `cratis.event_store.name` |
| `Namespace` | `cratis.event_store.namespace` |
| `ObserverId` | `cratis.observer.id` |
| `EventSequenceId` | `cratis.event_sequence.id` |
| `eventName` | `cratis.event_type.id` |
| `QueueId` | `cratis.chronicle.appended_events_queue.id` |
| `Reason` | `cratis.chronicle.observer.spill_reason` |
| `divergence` | `cratis.chronicle.observer.claim_divergence` |
| `Condition` | `cratis.chronicle.alert.condition` |
| `Severity` | `cratis.chronicle.alert.severity` |
| `state` | `cratis.chronicle.abandoned_delivery.result_state` |
| `authority` | `cratis.chronicle.webhook.authority` |
| `provider` | `cratis.chronicle.sql.provider` |
| `outcome` | `cratis.chronicle.sql.schema_lock.outcome` |
| `query` | `cratis.chronicle.sql.live_query.name` |
| `kind` | `cratis.chronicle.sql.live_query.failure_kind` |
| `operation` | `cratis.chronicle.sql.upsert.operation` |

## Span names and attributes

Kernel span names already use `cratis.chronicle.*`. Their names and kinds are unchanged. Client operation and display names remain the existing `client.*` names by default. In .NET, set `ChronicleTelemetryOptions.SpanNames` to `ClientSpanNames.Convention` through `WithTelemetry` to opt in, or configure `Telemetry:SpanNames` as `Convention`. The activity source remains `Cratis.Chronicle.Client`. Each operation emits one span, never a legacy and a convention span together.

| Default client span | Convention client span |
| --- | --- |
| `client.event_sequence.append` | `cratis.chronicle.client.event_sequence.append` |
| `client.event_sequence.append_many` | `cratis.chronicle.client.event_sequence.append_many` |
| `client.unit_of_work.commit` | `cratis.chronicle.client.unit_of_work.commit` |
| `client.unit_of_work.rollback` | `cratis.chronicle.client.unit_of_work.rollback` |
| `client.reactor.handle` | `cratis.chronicle.client.reactor.handle` |
| `client.reducer.handle` | `cratis.chronicle.client.reducer.handle` |

Kernel and client spans carry the shared keys alongside their existing keys in either client naming mode. Kernel `cratis.eventstore.name`, `cratis.eventstore.namespace`, `cratis.eventsequence.id`, `cratis.eventsource.type`, `cratis.eventsource.id`, `cratis.eventtype.id`, and `cratis.connection.id` gain `cratis.event_store.name`, `cratis.event_store.namespace`, `cratis.event_sequence.id`, `cratis.event_source.type`, `cratis.event_source.id`, `cratis.event_type.id`, and `cratis.chronicle.connection.id`, respectively. Observer keys already match the convention.

Client `event_store_name`, `namespace_name`, `event_sequence_id`, `event_source_type`, `event_source_id`, and `correlation_id` gain the corresponding shared keys. Reactor and reducer ids gain `cratis.observer.id` and `cratis.observer.type`. [Identifier exposure configuration](telemetry-identifiers.md) applies to both copies of an event source id: hashed values are identical under both keys, and omitted ids appear under neither key.
