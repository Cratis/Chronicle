// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Describes a legacy metric, published next to its convention counterpart for one minor release.
/// </summary>
/// <param name="Name">The legacy instrument name.</param>
/// <param name="Unit">The legacy unit, or null.</param>
/// <param name="Description">The legacy description, or null.</param>
internal record LegacyMetric(string Name, string? Unit, string? Description);

/// <summary>
/// The legacy telemetry names that are still published for one minor release next to the names in <see cref="WellKnownMetricNames"/>.
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
        [WellKnownMetricNames.ObserverSuccessfulObservations] = [new("chronicle-observer-successful-observations", null, "Number of successful observations per observer in a given event store and namespace")],
        [WellKnownMetricNames.ObserverPartitionFailures] = [new("chronicle-observer-partitions-failed", null, "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt")],
        [WellKnownMetricNames.ObserverPartitionsQuarantined] = [new("chronicle-observer-partitions-quarantined", null, "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see chronicle-observer-quarantined")],
        [WellKnownMetricNames.ObserverPartitionQuarantinesCleared] = [new("chronicle-observer-partition-quarantines-cleared", null, "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition")],
        [WellKnownMetricNames.ObserverQuarantines] = [new("chronicle-observer-quarantined", null, "Number of times an observer was quarantined")],
        [WellKnownMetricNames.ObserverSuspectedSkippedEvents] = [new("chronicle-observer-events-suspected-skipped", null, "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof")],
        [WellKnownMetricNames.ObserverZeroWorkCatchUps] = [new("chronicle-observer-zero-work-catch-ups", null, "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position")],
        [WellKnownMetricNames.ObserverClaimDivergences] = [new("chronicle-observer-claim-divergences", null, "Shadow partition-claim divergences from replaying/catching-up sets")],
        [WellKnownMetricNames.WebhookProbesScheduled] = [new("chronicle-webhook-probe-scheduled", null, null)],
        [WellKnownMetricNames.WebhookProbeAttempts] = [new("chronicle-webhook-probe-attempts", null, null)],
        [WellKnownMetricNames.WebhookProbeFailures] = [new("chronicle-webhook-probe-failures", null, null)],
        [WellKnownMetricNames.WebhookProbeSuccesses] = [new("chronicle-webhook-probe-successes", null, null)],
        [WellKnownMetricNames.WebhookProbeResumes] = [new("chronicle-webhook-probe-resumes", null, null)],
        [WellKnownMetricNames.WebhookProbesDeferred] = [new("chronicle-webhook-probe-deferred", null, null)],
        [WellKnownMetricNames.WebhookProbesSkipped] = [new("chronicle-webhook-probe-skipped", null, null)],
        [WellKnownMetricNames.WebhookProbesCancelled] = [new("chronicle-webhook-probe-cancelled", null, null)],
        [WellKnownMetricNames.WebhookCircuitOpenings] = [new("chronicle-webhook-circuit-opened", null, "Number of webhook circuit openings per target authority")],
        [WellKnownMetricNames.WebhookCircuitRejections] = [new("chronicle-webhook-circuit-rejected", null, "Number of webhook deliveries deferred because the target circuit is open")],
        [WellKnownMetricNames.EventSequenceAppendedEvents] = [new("chronicle-event-sequences-appended-events", null, "Number of events appended to the event sequence")],
        [WellKnownMetricNames.EventSequenceDuplicateSequenceNumbers] = [new("chronicle-event-sequences-duplicate-event-sequence-numbers", null, "Number of duplicate event sequence numbers")],
        [WellKnownMetricNames.EventSequenceAppendFailures] = [new("chronicle-event-sequences-failed-appended-events", null, "Number of events that failed to be appended to the event sequence")],
        [WellKnownMetricNames.EventSequenceConstraintViolations] = [new("chronicle-event-sequences-constraint-violations", null, "Number of constraint violations")],
        [WellKnownMetricNames.EventSequenceUnknownAppendOutcomes] = [new("chronicle-event-sequences-append-outcomes-unknown", null, "Number of appends whose storage outcome could not be settled")],
        [WellKnownMetricNames.ConstraintIndexUpdateFailures] = [new("chronicle-event-sequences-constraint-index-update-failures", null, "Number of failed constraint index attempts")],
        [WellKnownMetricNames.ConstraintIndexSettledErrors] = [new("chronicle-event-sequences-constraint-index-settled-errors", null, "Number of constraint index events settled with a deterministic error")],
        [WellKnownMetricNames.ConstraintIndexPoisonEvents] = [new("chronicle-event-sequences-constraint-index-poison-events", null, "Number of constraint event effects settled after five identical non-transient failures")],
        [WellKnownMetricNames.ConstraintIndexRepairs] = [new("chronicle-event-sequences-constraint-index-repairs", null, "Number of completed constraint index repairs")],
        [WellKnownMetricNames.ConstraintIndexRefusals] = [new("chronicle-event-sequences-constraint-index-refusals", null, "Number of appends refused for pending constraint indexes")],
        [WellKnownMetricNames.ConstraintIndexLag] = [new("chronicle-event-sequences-constraint-index-lag", null, "Events not yet covered by confirmed constraint updates")],
        [WellKnownMetricNames.AppendedEventsQueueEnqueuedEvents] = [new("chronicle-appended-events-queue-events-enqueued", null, "Number of events enqueued")],
        [WellKnownMetricNames.AppendedEventsQueueHandledEvents] = [new("chronicle-appended-events-queue-events-handled", null, "Number of events processed")],
        [WellKnownMetricNames.AppendedEventsQueueHandlingFailures] = [new("chronicle-appended-events-queue-events-handling-failures", null, "Number of events that has handling failures")],
        [WellKnownMetricNames.AppendedEventsQueueSpilledEvents] = [new("chronicle-appended-events-queue-events-spilled-to-catchup", null, "Number of events spilled to observer catch-up when the queue was full")],
        [WellKnownMetricNames.AppendedEventsQueueCatchUpTriggerBudgetExceeded] = [new("chronicle-appended-events-queue-catchup-trigger-budget-exceeded", null, "Number of spilled observer catch-up triggers still failing after the warning budget")],
        [WellKnownMetricNames.AppendedEventsQueueSuspectedUndeliveredEvents] = [new("chronicle-appended-events-queue-suspected-undelivered-events", null, "Number of event deliveries to an observer whose dispatch failed; one event can count once per observer")],
        [WellKnownMetricNames.AppendedEventsQueueObserverSpills] = [new("chronicle-appended-events-queue-observers-spilled-to-catchup", null, "Number of times an observer was moved from live delivery to catch-up, by reason")],
        [WellKnownMetricNames.AlertFailedTransitions] = [new("chronicle-alert-transitions-failed", null, "Number of failed attempts to record observer alert transitions")],
        [WellKnownMetricNames.AbandonedDeliveryLateResults] = [new("abandoned-delivery-late-results", null, "Client results received after the delivery deadline")],
        [WellKnownMetricNames.AbandonedDeliveryHoldExpirations] = [new("abandoned-delivery-hold-expirations", null, "Client deliveries released by the maximum hold")],
        [WellKnownMetricNames.AbandonedDeliveryFenceDuration] = [new("abandoned-delivery-fence-duration", "s", "Time an abandoned client delivery remained held")],
        [WellKnownMetricNames.ConnectedClientCount] = [new("chronicle-connected-clients", null, "Number of connected clients")],
        [WellKnownMetricNames.SqlSchemaLockWaitDuration] = [new("chronicle-sql-schema-lock-wait", "s", "Time spent waiting for a database schema lock")],
        [WellKnownMetricNames.SqlLiveQueryPollFailures] = [new("chronicle-sql-live-query-poll-failures", null, "Failed SQL live-query polls; each is retried")],
        [WellKnownMetricNames.SqlUpsertRecoveredCollisions] = [new("chronicle-sql-upsert-collisions-recovered", null, "Concurrent SQL insert collisions recovered by storage")],
        [WellKnownMetricNames.SqlCompositeKeyStampFailures] = [new("chronicle.sql.composite_key.stamp_failures", null, null)],
        [WellKnownMetricNames.SqlCompositeKeyCollisions] = [new("chronicle.sql.composite_key.collisions", null, null)],
    };

    /// <summary>
    /// Gets the metrics that are new in the convention names and have no legacy counterpart to publish. The open incident
    /// gauges are observable, so they are registered twice by the gauge itself instead of being bridged.
    /// </summary>
    internal static readonly IReadOnlySet<string> MetricsWithoutBridge = new HashSet<string>
    {
        WellKnownMetricNames.AlertOpenIncidents,
        WellKnownMetricNames.AlertOpenIncidentsAvailable
    };

    /// <summary>
    /// Gets the legacy metric attribute keys keyed by the registry attribute key.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> MetricAttributeKeys = new Dictionary<string, string>
    {
        [WellKnownTelemetryAttributes.EventStoreName] = "EventStore",
        [WellKnownTelemetryAttributes.EventStoreNamespace] = "Namespace",
        [WellKnownTelemetryAttributes.ObserverId] = "ObserverId",
        [WellKnownTelemetryAttributes.EventSequenceId] = "EventSequenceId",
        [WellKnownTelemetryAttributes.EventTypeId] = "eventName",
        [WellKnownTelemetryAttributes.AppendedEventsQueueId] = "QueueId",
        [WellKnownTelemetryAttributes.ObserverSpillReason] = "Reason",
        [WellKnownTelemetryAttributes.AlertCondition] = "Condition",
        [WellKnownTelemetryAttributes.AlertSeverity] = "Severity",
        [WellKnownTelemetryAttributes.AbandonedDeliveryResultState] = "state",
        [WellKnownTelemetryAttributes.ObserverClaimDivergence] = "divergence",
        [WellKnownTelemetryAttributes.WebhookAuthority] = "authority",
        [WellKnownTelemetryAttributes.SqlProvider] = "provider",
        [WellKnownTelemetryAttributes.SqlSchemaLockOutcome] = "outcome",
        [WellKnownTelemetryAttributes.SqlLiveQueryName] = "query",
        [WellKnownTelemetryAttributes.SqlLiveQueryFailureKind] = "kind",
        [WellKnownTelemetryAttributes.SqlUpsertOperation] = "operation",
    };

    /// <summary>
    /// Gets the legacy span attribute keys keyed by the registry attribute key.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> SpanAttributeKeys = new Dictionary<string, string>
    {
        [WellKnownTelemetryAttributes.EventSourceType] = "cratis.eventsource.type",
        [WellKnownTelemetryAttributes.EventSourceId] = "cratis.eventsource.id",
        [WellKnownTelemetryAttributes.EventStoreName] = "cratis.eventstore.name",
        [WellKnownTelemetryAttributes.EventStoreNamespace] = "cratis.eventstore.namespace",
        [WellKnownTelemetryAttributes.EventSequenceId] = "cratis.eventsequence.id",
        [WellKnownTelemetryAttributes.ConnectionId] = "cratis.connection.id",
        [WellKnownTelemetryAttributes.EventTypeId] = "cratis.eventtype.id",
    };

    /// <summary>
    /// Translates a registry metric attribute key to its legacy key.
    /// </summary>
    /// <param name="key">The registry key.</param>
    /// <returns>The legacy key, or the key itself when it has no legacy key.</returns>
    internal static string ToLegacyMetricKey(string key) => MetricAttributeKeys.TryGetValue(key, out var legacy) ? legacy : key;
}
