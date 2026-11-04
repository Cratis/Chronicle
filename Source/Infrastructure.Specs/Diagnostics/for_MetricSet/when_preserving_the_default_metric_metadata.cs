// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_preserving_the_default_metric_metadata : Specification
{
    /// <summary>
    /// Pins the metadata published on main before convention naming was introduced.
    /// </summary>
    static readonly LegacyMetric[] _expected =
    [
        new("chronicle-observer-successful-observations", null, "Number of successful observations per observer in a given event store and namespace"),
        new("chronicle-observer-partitions-failed", null, "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt"),
        new("chronicle-observer-partitions-quarantined", null, "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see chronicle-observer-quarantined"),
        new("chronicle-observer-partition-quarantines-cleared", null, "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition"),
        new("chronicle-observer-quarantined", null, "Number of times an observer was quarantined"),
        new("chronicle-observer-events-suspected-skipped", null, "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof"),
        new("chronicle-observer-zero-work-catch-ups", null, "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position"),
        new("chronicle-observer-claim-divergences", null, "Shadow partition-claim divergences from replaying/catching-up sets"),
        new("chronicle-webhook-probe-scheduled", null, null),
        new("chronicle-webhook-probe-attempts", null, null),
        new("chronicle-webhook-probe-failures", null, null),
        new("chronicle-webhook-probe-successes", null, null),
        new("chronicle-webhook-probe-resumes", null, null),
        new("chronicle-webhook-probe-deferred", null, null),
        new("chronicle-webhook-probe-skipped", null, null),
        new("chronicle-webhook-probe-cancelled", null, null),
        new("chronicle-webhook-circuit-opened", null, "Number of webhook circuit openings per target authority"),
        new("chronicle-webhook-circuit-rejected", null, "Number of webhook deliveries deferred because the target circuit is open"),
        new("chronicle-event-sequences-appended-events", null, "Number of events appended to the event sequence"),
        new("chronicle-event-sequences-duplicate-event-sequence-numbers", null, "Number of duplicate event sequence numbers"),
        new("chronicle-event-sequences-failed-appended-events", null, "Number of events that failed to be appended to the event sequence"),
        new("chronicle-event-sequences-constraint-violations", null, "Number of constraint violations"),
        new("chronicle-event-sequences-append-outcomes-unknown", null, "Number of appends whose storage outcome could not be settled"),
        new("chronicle-event-sequences-constraint-indexes-rebuilding", null, "Constraints whose index is held as rebuilding and refuses governed appends"),
        new("chronicle-event-sequences-constraint-index-rebuild-ends", null, "Ended constraint index rebuilds by handoff reason"),
        new("chronicle-event-sequences-constraint-index-update-failures", null, "Number of failed constraint index attempts"),
        new("chronicle-event-sequences-constraint-index-settled-errors", null, "Number of constraint index events settled with a deterministic error"),
        new("chronicle-event-sequences-constraint-index-poison-events", null, "Number of constraint event effects settled after five identical non-transient failures"),
        new("chronicle-event-sequences-constraint-index-repairs", null, "Number of completed constraint index repairs"),
        new("chronicle-event-sequences-constraint-index-refusals", null, "Number of appends refused for pending constraint indexes"),
        new("chronicle-event-sequences-constraint-index-lag", null, "Events not yet covered by confirmed constraint updates"),
        new("chronicle-appended-events-queue-events-enqueued", null, "Number of events enqueued"),
        new("chronicle-appended-events-queue-events-handled", null, "Number of events processed"),
        new("chronicle-appended-events-queue-events-handling-failures", null, "Number of events that has handling failures"),
        new("chronicle-appended-events-queue-events-spilled-to-catchup", null, "Number of events spilled to observer catch-up when the queue was full"),
        new("chronicle-appended-events-queue-catchup-trigger-budget-exceeded", null, "Number of spilled observer catch-up triggers still failing after the warning budget"),
        new("chronicle-appended-events-queue-suspected-undelivered-events", null, "Number of event deliveries to an observer whose dispatch failed; one event can count once per observer"),
        new("chronicle-appended-events-queue-observers-spilled-to-catchup", null, "Number of times an observer was moved from live delivery to catch-up, by reason"),
        new("chronicle-alert-transitions-failed", null, "Number of failed attempts to record observer alert transitions"),
        new("abandoned-delivery-late-results", null, "Client results received after the delivery deadline"),
        new("abandoned-delivery-hold-expirations", null, "Client deliveries released by the maximum hold"),
        new("abandoned-delivery-fence-duration", "s", "Time an abandoned client delivery remained held"),
        new("chronicle-connected-clients", null, "Number of connected clients"),
        new("chronicle-sql-schema-lock-wait", "s", "Time spent waiting for a database schema lock"),
        new("chronicle-sql-live-query-poll-failures", null, "Failed SQL live-query polls; each is retried"),
        new("chronicle-sql-upsert-collisions-recovered", null, "Concurrent SQL insert collisions recovered by storage"),
        new("chronicle.sql.composite_key.stamp_failures", null, null),
        new("chronicle.sql.composite_key.collisions", null, null)
    ];

    [Fact] void should_preserve_every_legacy_instrument() => LegacyTelemetryNames.Metrics.Values.SelectMany(_ => _).OrderBy(_ => _.Name).ShouldEqual(_expected.OrderBy(_ => _.Name));

    [Fact]
    void should_preserve_every_legacy_dimension_key() => LegacyTelemetryNames.MetricAttributeKeys.OrderBy(_ => _.Key).ShouldEqual(new Dictionary<string, string>
    {
        ["cratis.event_store.name"] = "EventStore",
        ["cratis.event_store.namespace"] = "Namespace",
        ["cratis.observer.id"] = "ObserverId",
        ["cratis.event_sequence.id"] = "EventSequenceId",
        ["cratis.event_type.id"] = "eventName",
        ["cratis.chronicle.appended_events_queue.id"] = "QueueId",
        ["cratis.chronicle.observer.spill_reason"] = "Reason",
        ["cratis.chronicle.alert.condition"] = "Condition",
        ["cratis.chronicle.alert.severity"] = "Severity",
        ["cratis.chronicle.abandoned_delivery.result_state"] = "state",
        ["cratis.chronicle.observer.claim_divergence"] = "divergence",
        ["cratis.chronicle.webhook.authority"] = "authority",
        ["cratis.chronicle.sql.provider"] = "provider",
        ["cratis.chronicle.sql.schema_lock.outcome"] = "outcome",
        ["cratis.chronicle.sql.live_query.name"] = "query",
        ["cratis.chronicle.sql.live_query.failure_kind"] = "kind",
        ["cratis.chronicle.sql.upsert.operation"] = "operation"
    }.OrderBy(_ => _.Key));
}
