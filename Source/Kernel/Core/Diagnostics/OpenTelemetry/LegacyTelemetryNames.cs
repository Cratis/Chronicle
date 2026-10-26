// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

using Names = Cratis.Chronicle.Diagnostics.WellKnownTelemetryNames;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Describes a legacy metric, published next to its convention counterpart for one minor release.
/// </summary>
/// <param name="Name">The legacy instrument name.</param>
/// <param name="Unit">The legacy unit, or null.</param>
/// <param name="Description">The legacy description, or null.</param>
internal record LegacyMetric(string Name, string? Unit, string? Description);

/// <summary>
/// The legacy telemetry names that are still published for one minor release next to the names in <see cref="WellKnownTelemetryNames"/>.
/// All of this is removed in the next major version.
/// </summary>
internal static class LegacyTelemetryNames
{
    /// <summary>
    /// The name of the legacy retry attempt metric. It repeated the partition failure metric and is not part of the convention names.
    /// </summary>
    internal const string PartitionRetryAttempts = "chronicle-observer-partition-retry-attempts";

    /// <summary>
    /// The description of <see cref="PartitionRetryAttempts"/>.
    /// </summary>
    internal const string PartitionRetryAttemptsDescription = "Number of times a failed partition was evaluated for retry per observer. Incremented on every partition failure, whether or not a retry ends up being scheduled, so it does not count retries that actually ran";

    /// <summary>
    /// The name of the legacy open incidents gauge.
    /// </summary>
    internal const string OpenIncidents = "chronicle-alerts-open-incidents";

    /// <summary>
    /// The name of the legacy open incidents availability gauge.
    /// </summary>
    internal const string OpenIncidentsAvailable = "chronicle-alerts-open-incidents-available";

    /// <summary>
    /// The names of the meters that carry kernel metrics.
    /// </summary>
    internal static readonly string[] Meters = [WellKnown.MeterName, "Cratis.Chronicle.Storage.Sql.CompositeKeyShadow"];

    /// <summary>
    /// Gets the legacy metrics keyed by the convention metric name. Observable gauges are not bridged and are registered twice instead.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, LegacyMetric[]> Metrics = new Dictionary<string, LegacyMetric[]>
    {
        [Names.Metrics.ObserverSuccessfulObservations] = [new("chronicle-observer-successful-observations", null, "Number of successful observations per observer in a given event store and namespace")],
        [Names.Metrics.ObserverPartitionFailures] = [new("chronicle-observer-partitions-failed", null, "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt")],
        [Names.Metrics.ObserverPartitionsQuarantined] = [new("chronicle-observer-partitions-quarantined", null, "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see chronicle-observer-quarantined")],
        [Names.Metrics.ObserverPartitionQuarantinesCleared] = [new("chronicle-observer-partition-quarantines-cleared", null, "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition")],
        [Names.Metrics.ObserverQuarantines] = [new("chronicle-observer-quarantined", null, "Number of times an observer was quarantined")],
        [Names.Metrics.ObserverSuspectedSkippedEvents] = [new("chronicle-observer-events-suspected-skipped", null, "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof")],
        [Names.Metrics.ObserverZeroWorkCatchUps] = [new("chronicle-observer-zero-work-catch-ups", null, "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position")],
        [Names.Metrics.ObserverClaimDivergences] = [new("chronicle-observer-claim-divergences", null, "Shadow partition-claim divergences from replaying/catching-up sets")],
        [Names.Metrics.WebhookProbesScheduled] = [new("chronicle-webhook-probe-scheduled", null, null)],
        [Names.Metrics.WebhookProbeAttempts] = [new("chronicle-webhook-probe-attempts", null, null)],
        [Names.Metrics.WebhookProbeFailures] = [new("chronicle-webhook-probe-failures", null, null)],
        [Names.Metrics.WebhookProbeSuccesses] = [new("chronicle-webhook-probe-successes", null, null)],
        [Names.Metrics.WebhookProbeResumes] = [new("chronicle-webhook-probe-resumes", null, null)],
        [Names.Metrics.WebhookProbesDeferred] = [new("chronicle-webhook-probe-deferred", null, null)],
        [Names.Metrics.WebhookProbesSkipped] = [new("chronicle-webhook-probe-skipped", null, null)],
        [Names.Metrics.WebhookProbesCancelled] = [new("chronicle-webhook-probe-cancelled", null, null)],
        [Names.Metrics.WebhookCircuitOpenings] = [new("chronicle-webhook-circuit-opened", null, "Number of webhook circuit openings per target authority")],
        [Names.Metrics.WebhookCircuitRejections] = [new("chronicle-webhook-circuit-rejected", null, "Number of webhook deliveries deferred because the target circuit is open")],
        [Names.Metrics.EventSequenceAppendedEvents] = [new("chronicle-event-sequences-appended-events", null, "Number of events appended to the event sequence")],
        [Names.Metrics.EventSequenceDuplicateSequenceNumbers] = [new("chronicle-event-sequences-duplicate-event-sequence-numbers", null, "Number of duplicate event sequence numbers")],
        [Names.Metrics.EventSequenceAppendFailures] = [new("chronicle-event-sequences-failed-appended-events", null, "Number of events that failed to be appended to the event sequence")],
        [Names.Metrics.EventSequenceConstraintViolations] = [new("chronicle-event-sequences-constraint-violations", null, "Number of constraint violations")],
        [Names.Metrics.EventSequenceUnknownAppendOutcomes] = [new("chronicle-event-sequences-append-outcomes-unknown", null, "Number of appends whose storage outcome could not be settled")],
        [Names.Metrics.ConstraintIndexUpdateFailures] = [new("chronicle-event-sequences-constraint-index-update-failures", null, "Number of failed constraint index attempts")],
        [Names.Metrics.ConstraintIndexSettledErrors] = [new("chronicle-event-sequences-constraint-index-settled-errors", null, "Number of constraint index events settled with a deterministic error")],
        [Names.Metrics.ConstraintIndexPoisonEvents] = [new("chronicle-event-sequences-constraint-index-poison-events", null, "Number of constraint event effects settled after five identical non-transient failures")],
        [Names.Metrics.ConstraintIndexRepairs] = [new("chronicle-event-sequences-constraint-index-repairs", null, "Number of completed constraint index repairs")],
        [Names.Metrics.ConstraintIndexRefusals] = [new("chronicle-event-sequences-constraint-index-refusals", null, "Number of appends refused for pending constraint indexes")],
        [Names.Metrics.ConstraintIndexLag] = [new("chronicle-event-sequences-constraint-index-lag", null, "Events not yet covered by confirmed constraint updates")],
        [Names.Metrics.AppendedEventsQueueEnqueuedEvents] = [new("chronicle-appended-events-queue-events-enqueued", null, "Number of events enqueued")],
        [Names.Metrics.AppendedEventsQueueHandledEvents] = [new("chronicle-appended-events-queue-events-handled", null, "Number of events processed")],
        [Names.Metrics.AppendedEventsQueueHandlingFailures] = [new("chronicle-appended-events-queue-events-handling-failures", null, "Number of events that has handling failures")],
        [Names.Metrics.AppendedEventsQueueSpilledEvents] = [new("chronicle-appended-events-queue-events-spilled-to-catchup", null, "Number of events spilled to observer catch-up when the queue was full")],
        [Names.Metrics.AppendedEventsQueueCatchUpTriggerBudgetExceeded] = [new("chronicle-appended-events-queue-catchup-trigger-budget-exceeded", null, "Number of spilled observer catch-up triggers still failing after the warning budget")],
        [Names.Metrics.AppendedEventsQueueSuspectedUndeliveredEvents] = [new("chronicle-appended-events-queue-suspected-undelivered-events", null, "Number of event deliveries to an observer whose dispatch failed; one event can count once per observer")],
        [Names.Metrics.AppendedEventsQueueObserverSpills] = [new("chronicle-appended-events-queue-observers-spilled-to-catchup", null, "Number of times an observer was moved from live delivery to catch-up, by reason")],
        [Names.Metrics.AlertFailedTransitions] = [new("chronicle-alert-transitions-failed", null, "Number of failed attempts to record observer alert transitions")],
        [Names.Metrics.AbandonedDeliveryLateResults] = [new("abandoned-delivery-late-results", null, "Client results received after the delivery deadline")],
        [Names.Metrics.AbandonedDeliveryHoldExpirations] = [new("abandoned-delivery-hold-expirations", null, "Client deliveries released by the maximum hold")],
        [Names.Metrics.AbandonedDeliveryFenceDuration] = [new("abandoned-delivery-fence-duration", "s", "Time an abandoned client delivery remained held")],
        [Names.Metrics.ConnectedClientCount] = [new("chronicle-connected-clients", null, "Number of connected clients")],
        [Names.Metrics.SqlSchemaLockWaitDuration] = [new("chronicle-sql-schema-lock-wait", "s", "Time spent waiting for a database schema lock")],
        [Names.Metrics.SqlLiveQueryPollFailures] = [new("chronicle-sql-live-query-poll-failures", null, "Failed SQL live-query polls; each is retried")],
        [Names.Metrics.SqlUpsertRecoveredCollisions] = [new("chronicle-sql-upsert-collisions-recovered", null, "Concurrent SQL insert collisions recovered by storage")],
        [Names.Metrics.SqlCompositeKeyStampFailures] = [new("chronicle.sql.composite_key.stamp_failures", null, null)],
        [Names.Metrics.SqlCompositeKeyCollisions] = [new("chronicle.sql.composite_key.collisions", null, null)],
    };

    /// <summary>
    /// Gets the metrics that are new in the convention names and have no legacy counterpart to publish. The open incident
    /// gauges are observable, so they are registered twice by the gauge itself instead of being bridged.
    /// </summary>
    internal static readonly IReadOnlySet<string> MetricsWithoutBridge = new HashSet<string>
    {
        Names.Metrics.AlertOpenIncidents,
        Names.Metrics.AlertOpenIncidentsAvailable
    };

    /// <summary>
    /// Gets the legacy metric attribute keys keyed by the registry attribute key.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> MetricAttributeKeys = new Dictionary<string, string>
    {
        [Names.Attributes.EventStoreName] = "EventStore",
        [Names.Attributes.EventStoreNamespace] = "Namespace",
        [Names.Attributes.ObserverId] = "ObserverId",
        [Names.Attributes.EventSequenceId] = "EventSequenceId",
        [Names.Attributes.EventTypeId] = "eventName",
        [Names.Attributes.AppendedEventsQueueId] = "QueueId",
        [Names.Attributes.ObserverSpillReason] = "Reason",
        [Names.Attributes.AlertCondition] = "Condition",
        [Names.Attributes.AlertSeverity] = "Severity",
        [Names.Attributes.AbandonedDeliveryResultState] = "state",
        [Names.Attributes.ObserverClaimDivergence] = "divergence",
        [Names.Attributes.WebhookAuthority] = "authority",
        [Names.Attributes.SqlProvider] = "provider",
        [Names.Attributes.SqlSchemaLockOutcome] = "outcome",
        [Names.Attributes.SqlLiveQueryName] = "query",
        [Names.Attributes.SqlLiveQueryFailureKind] = "kind",
        [Names.Attributes.SqlUpsertOperation] = "operation",
    };

    /// <summary>
    /// Gets the legacy span attribute keys keyed by the registry attribute key.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> SpanAttributeKeys = new Dictionary<string, string>
    {
        [Names.Attributes.EventSourceType] = "cratis.eventsource.type",
        [Names.Attributes.EventSourceId] = "cratis.eventsource.id",
        [Names.Attributes.EventStoreName] = "cratis.eventstore.name",
        [Names.Attributes.EventStoreNamespace] = "cratis.eventstore.namespace",
        [Names.Attributes.EventSequenceId] = "cratis.eventsequence.id",
        [Names.Attributes.ConnectionId] = "cratis.connection.id",
        [Names.Attributes.EventTypeId] = "cratis.eventtype.id",
    };

    /// <summary>
    /// Translates a registry metric attribute key to its legacy key.
    /// </summary>
    /// <param name="key">The registry key.</param>
    /// <returns>The legacy key, or the key itself when it has no legacy key.</returns>
    internal static string ToLegacyMetricKey(string key) => MetricAttributeKeys.TryGetValue(key, out var legacy) ? legacy : key;
}
