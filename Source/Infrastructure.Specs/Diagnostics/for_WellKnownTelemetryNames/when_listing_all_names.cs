// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Diagnostics.for_WellKnownTelemetryNames;

public class when_listing_all_names : given.all_telemetry_names
{
    static readonly Regex _nameFormat = new(@"^cratis\.chronicle(\.[a-z][a-z0-9_]*)+$", RegexOptions.Compiled);
    static readonly Regex _attributeFormat = new(@"^cratis(\.[a-z][a-z0-9_]*)+$", RegexOptions.Compiled);

    static readonly Dictionary<string, string> _expected = new()
    {
        ["Attributes.CorrelationId"] = "cratis.correlation_id",
        ["Attributes.EventStoreName"] = "cratis.event_store.name",
        ["Attributes.EventStoreNamespace"] = "cratis.event_store.namespace",
        ["Attributes.TenantId"] = "cratis.tenant.id",
        ["Attributes.EventSequenceId"] = "cratis.event_sequence.id",
        ["Attributes.EventSequenceNumber"] = "cratis.event_sequence.number",
        ["Attributes.EventTypeId"] = "cratis.event_type.id",
        ["Attributes.EventTypeGeneration"] = "cratis.event_type.generation",
        ["Attributes.EventSourceType"] = "cratis.event_source.type",
        ["Attributes.EventSourceId"] = "cratis.event_source.id",
        ["Attributes.EventCount"] = "cratis.event.count",
        ["Attributes.ObserverId"] = "cratis.observer.id",
        ["Attributes.ObserverType"] = "cratis.observer.type",
        ["Attributes.Replay"] = "cratis.replay",
        ["Attributes.RetryAttempt"] = "cratis.retry.attempt",
        ["Attributes.ConnectionId"] = "cratis.chronicle.connection.id",
        ["Attributes.AppendedEventsQueueId"] = "cratis.chronicle.appended_events_queue.id",
        ["Attributes.ObserverSpillReason"] = "cratis.chronicle.observer.spill_reason",
        ["Attributes.ObserverClaimDivergence"] = "cratis.chronicle.observer.claim_divergence",
        ["Attributes.AlertCondition"] = "cratis.chronicle.alert.condition",
        ["Attributes.AlertSeverity"] = "cratis.chronicle.alert.severity",
        ["Attributes.AbandonedDeliveryResultState"] = "cratis.chronicle.abandoned_delivery.result_state",
        ["Attributes.WebhookAuthority"] = "cratis.chronicle.webhook.authority",
        ["Attributes.SqlProvider"] = "cratis.chronicle.sql.provider",
        ["Attributes.SqlSchemaLockOutcome"] = "cratis.chronicle.sql.schema_lock.outcome",
        ["Attributes.SqlLiveQueryName"] = "cratis.chronicle.sql.live_query.name",
        ["Attributes.SqlLiveQueryFailureKind"] = "cratis.chronicle.sql.live_query.failure_kind",
        ["Attributes.SqlUpsertOperation"] = "cratis.chronicle.sql.upsert.operation",
        ["Metrics.ObserverSuccessfulObservations"] = "cratis.chronicle.observer.successful_observations",
        ["Metrics.ObserverPartitionFailures"] = "cratis.chronicle.observer.partition_failures",
        ["Metrics.ObserverPartitionsQuarantined"] = "cratis.chronicle.observer.partitions_quarantined",
        ["Metrics.ObserverPartitionQuarantinesCleared"] = "cratis.chronicle.observer.partition_quarantines_cleared",
        ["Metrics.ObserverQuarantines"] = "cratis.chronicle.observer.quarantines",
        ["Metrics.ObserverSuspectedSkippedEvents"] = "cratis.chronicle.observer.suspected_skipped_events",
        ["Metrics.ObserverZeroWorkCatchUps"] = "cratis.chronicle.observer.zero_work_catch_ups",
        ["Metrics.ObserverClaimDivergences"] = "cratis.chronicle.observer.claim_divergences",
        ["Metrics.WebhookProbesScheduled"] = "cratis.chronicle.webhook.probes_scheduled",
        ["Metrics.WebhookProbeAttempts"] = "cratis.chronicle.webhook.probe_attempts",
        ["Metrics.WebhookProbeFailures"] = "cratis.chronicle.webhook.probe_failures",
        ["Metrics.WebhookProbeSuccesses"] = "cratis.chronicle.webhook.probe_successes",
        ["Metrics.WebhookProbeResumes"] = "cratis.chronicle.webhook.probe_resumes",
        ["Metrics.WebhookProbesDeferred"] = "cratis.chronicle.webhook.probes_deferred",
        ["Metrics.WebhookProbesSkipped"] = "cratis.chronicle.webhook.probes_skipped",
        ["Metrics.WebhookProbesCancelled"] = "cratis.chronicle.webhook.probes_cancelled",
        ["Metrics.WebhookCircuitOpenings"] = "cratis.chronicle.webhook.circuit_openings",
        ["Metrics.WebhookCircuitRejections"] = "cratis.chronicle.webhook.circuit_rejections",
        ["Metrics.EventSequenceAppendedEvents"] = "cratis.chronicle.event_sequence.appended_events",
        ["Metrics.EventSequenceDuplicateSequenceNumbers"] = "cratis.chronicle.event_sequence.duplicate_sequence_numbers",
        ["Metrics.EventSequenceAppendFailures"] = "cratis.chronicle.event_sequence.append_failures",
        ["Metrics.EventSequenceConstraintViolations"] = "cratis.chronicle.event_sequence.constraint_violations",
        ["Metrics.EventSequenceUnknownAppendOutcomes"] = "cratis.chronicle.event_sequence.unknown_append_outcomes",
        ["Metrics.ConstraintIndexUpdateFailures"] = "cratis.chronicle.constraint_index.update_failures",
        ["Metrics.ConstraintIndexSettledErrors"] = "cratis.chronicle.constraint_index.settled_errors",
        ["Metrics.ConstraintIndexPoisonEvents"] = "cratis.chronicle.constraint_index.poison_events",
        ["Metrics.ConstraintIndexRepairs"] = "cratis.chronicle.constraint_index.repairs",
        ["Metrics.ConstraintIndexRefusals"] = "cratis.chronicle.constraint_index.refusals",
        ["Metrics.ConstraintIndexLag"] = "cratis.chronicle.constraint_index.lag",
        ["Metrics.AppendedEventsQueueEnqueuedEvents"] = "cratis.chronicle.appended_events_queue.enqueued_events",
        ["Metrics.AppendedEventsQueueHandledEvents"] = "cratis.chronicle.appended_events_queue.handled_events",
        ["Metrics.AppendedEventsQueueHandlingFailures"] = "cratis.chronicle.appended_events_queue.handling_failures",
        ["Metrics.AppendedEventsQueueSpilledEvents"] = "cratis.chronicle.appended_events_queue.spilled_events",
        ["Metrics.AppendedEventsQueueCatchUpTriggerBudgetExceeded"] = "cratis.chronicle.appended_events_queue.catch_up_trigger_budget_exceeded",
        ["Metrics.AppendedEventsQueueSuspectedUndeliveredEvents"] = "cratis.chronicle.appended_events_queue.suspected_undelivered_events",
        ["Metrics.AppendedEventsQueueObserverSpills"] = "cratis.chronicle.appended_events_queue.observer_spills",
        ["Metrics.AlertFailedTransitions"] = "cratis.chronicle.alert.failed_transitions",
        ["Metrics.AlertOpenIncidents"] = "cratis.chronicle.alert.open_incidents",
        ["Metrics.AlertOpenIncidentsAvailable"] = "cratis.chronicle.alert.open_incidents_available",
        ["Metrics.AbandonedDeliveryLateResults"] = "cratis.chronicle.abandoned_delivery.late_results",
        ["Metrics.AbandonedDeliveryHoldExpirations"] = "cratis.chronicle.abandoned_delivery.hold_expirations",
        ["Metrics.AbandonedDeliveryFenceDuration"] = "cratis.chronicle.abandoned_delivery.fence_duration",
        ["Metrics.ConnectedClientCount"] = "cratis.chronicle.connected_client.count",
        ["Metrics.SqlSchemaLockWaitDuration"] = "cratis.chronicle.sql.schema_lock.wait_duration",
        ["Metrics.SqlLiveQueryPollFailures"] = "cratis.chronicle.sql.live_query.poll_failures",
        ["Metrics.SqlUpsertRecoveredCollisions"] = "cratis.chronicle.sql.upsert.recovered_collisions",
        ["Metrics.SqlCompositeKeyStampFailures"] = "cratis.chronicle.sql.composite_key.stamp_failures",
        ["Metrics.SqlCompositeKeyCollisions"] = "cratis.chronicle.sql.composite_key.collisions",
        ["Spans.EventSequenceAppend"] = "cratis.chronicle.event_sequence.append",
        ["Spans.EventSequenceAppendMany"] = "cratis.chronicle.event_sequence.append_many",
        ["Spans.ObserverHandle"] = "cratis.chronicle.observer.handle",
        ["Spans.AppendedEventsQueueEnqueue"] = "cratis.chronicle.appended_events_queue.enqueue",
        ["Spans.AppendedEventsQueueDispatch"] = "cratis.chronicle.appended_events_queue.dispatch",
        ["ClientSpans.EventSequenceAppend"] = "cratis.chronicle.client.event_sequence.append",
        ["ClientSpans.EventSequenceAppendMany"] = "cratis.chronicle.client.event_sequence.append_many",
        ["ClientSpans.UnitOfWorkCommit"] = "cratis.chronicle.client.unit_of_work.commit",
        ["ClientSpans.UnitOfWorkRollback"] = "cratis.chronicle.client.unit_of_work.rollback",
        ["ClientSpans.ReactorHandle"] = "cratis.chronicle.client.reactor.handle",
        ["ClientSpans.ReducerHandle"] = "cratis.chronicle.client.reducer.handle"
    };

    static readonly Dictionary<string, string> _expectedUnits = new()
    {
        ["Metrics.ObserverSuccessfulObservationsUnit"] = "{observation}",
        ["Metrics.ObserverPartitionFailuresUnit"] = "{failure}",
        ["Metrics.ObserverPartitionsQuarantinedUnit"] = "{partition}",
        ["Metrics.ObserverPartitionQuarantinesClearedUnit"] = "{partition}",
        ["Metrics.ObserverQuarantinesUnit"] = "{quarantine}",
        ["Metrics.ObserverSuspectedSkippedEventsUnit"] = "{event}",
        ["Metrics.ObserverZeroWorkCatchUpsUnit"] = "{catch_up}",
        ["Metrics.ObserverClaimDivergencesUnit"] = "{divergence}",
        ["Metrics.WebhookProbesScheduledUnit"] = "{probe}",
        ["Metrics.WebhookProbeAttemptsUnit"] = "{probe}",
        ["Metrics.WebhookProbeFailuresUnit"] = "{probe}",
        ["Metrics.WebhookProbeSuccessesUnit"] = "{probe}",
        ["Metrics.WebhookProbeResumesUnit"] = "{probe}",
        ["Metrics.WebhookProbesDeferredUnit"] = "{probe}",
        ["Metrics.WebhookProbesSkippedUnit"] = "{probe}",
        ["Metrics.WebhookProbesCancelledUnit"] = "{probe}",
        ["Metrics.WebhookCircuitOpeningsUnit"] = "{opening}",
        ["Metrics.WebhookCircuitRejectionsUnit"] = "{delivery}",
        ["Metrics.EventSequenceAppendedEventsUnit"] = "{event}",
        ["Metrics.EventSequenceDuplicateSequenceNumbersUnit"] = "{event}",
        ["Metrics.EventSequenceAppendFailuresUnit"] = "{event}",
        ["Metrics.EventSequenceConstraintViolationsUnit"] = "{violation}",
        ["Metrics.EventSequenceUnknownAppendOutcomesUnit"] = "{append}",
        ["Metrics.ConstraintIndexUpdateFailuresUnit"] = "{failure}",
        ["Metrics.ConstraintIndexSettledErrorsUnit"] = "{event}",
        ["Metrics.ConstraintIndexPoisonEventsUnit"] = "{event}",
        ["Metrics.ConstraintIndexRepairsUnit"] = "{repair}",
        ["Metrics.ConstraintIndexRefusalsUnit"] = "{append}",
        ["Metrics.ConstraintIndexLagUnit"] = "{event}",
        ["Metrics.AppendedEventsQueueEnqueuedEventsUnit"] = "{event}",
        ["Metrics.AppendedEventsQueueHandledEventsUnit"] = "{event}",
        ["Metrics.AppendedEventsQueueHandlingFailuresUnit"] = "{failure}",
        ["Metrics.AppendedEventsQueueSpilledEventsUnit"] = "{event}",
        ["Metrics.AppendedEventsQueueCatchUpTriggerBudgetExceededUnit"] = "{trigger}",
        ["Metrics.AppendedEventsQueueSuspectedUndeliveredEventsUnit"] = "{event}",
        ["Metrics.AppendedEventsQueueObserverSpillsUnit"] = "{spill}",
        ["Metrics.AlertFailedTransitionsUnit"] = "{transition}",
        ["Metrics.AlertOpenIncidentsUnit"] = "{incident}",
        ["Metrics.AlertOpenIncidentsAvailableUnit"] = "1",
        ["Metrics.AbandonedDeliveryLateResultsUnit"] = "{result}",
        ["Metrics.AbandonedDeliveryHoldExpirationsUnit"] = "{delivery}",
        ["Metrics.AbandonedDeliveryFenceDurationUnit"] = "s",
        ["Metrics.ConnectedClientCountUnit"] = "{client}",
        ["Metrics.SqlSchemaLockWaitDurationUnit"] = "s",
        ["Metrics.SqlLiveQueryPollFailuresUnit"] = "{poll}",
        ["Metrics.SqlUpsertRecoveredCollisionsUnit"] = "{collision}",
        ["Metrics.SqlCompositeKeyStampFailuresUnit"] = "{failure}",
        ["Metrics.SqlCompositeKeyCollisionsUnit"] = "{collision}"
    };

    static string Render(IEnumerable<KeyValuePair<string, string>> names) =>
        string.Join("\n", names.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}={_.Value}"));

    [Fact] void should_publish_exactly_the_pinned_names() =>
        Render(_names.Where(_ => !IsUnit(_.Key))).ShouldEqual(Render(_expected));

    [Fact] void should_publish_exactly_the_pinned_units() =>
        Render(_names.Where(_ => IsUnit(_.Key))).ShouldEqual(Render(_expectedUnits));

    [Fact] void should_name_metrics_and_spans_in_the_convention_format() =>
        _names.Where(_ => !IsUnit(_.Key) && !_.Key.StartsWith("Attributes.")).All(_ => _nameFormat.IsMatch(_.Value)).ShouldBeTrue();

    [Fact] void should_name_attribute_keys_in_the_convention_format() =>
        In("Attributes").All(_ => _attributeFormat.IsMatch(_.Value)).ShouldBeTrue();

    [Fact] void should_have_unique_metric_names() =>
        In("Metrics").Select(_ => _.Value).Distinct().Count().ShouldEqual(In("Metrics").Count());

    [Fact] void should_have_unique_span_names() =>
        In("Spans").Select(_ => _.Value).Distinct().Count().ShouldEqual(In("Spans").Count());

    [Fact] void should_have_unique_client_span_names() =>
        In("ClientSpans").Select(_ => _.Value).Distinct().Count().ShouldEqual(In("ClientSpans").Count());

    [Fact] void should_have_unique_attribute_keys() =>
        In("Attributes").Select(_ => _.Value).Distinct().Count().ShouldEqual(In("Attributes").Count());

    [Fact] void should_not_publish_kernel_names_under_the_client_prefix() =>
        In("Metrics").Concat(In("Spans")).Any(_ => _.Value.StartsWith("cratis.chronicle.client.")).ShouldBeFalse();

    [Fact] void should_publish_client_span_names_under_the_client_prefix() =>
        In("ClientSpans").All(_ => _.Value.StartsWith("cratis.chronicle.client.")).ShouldBeTrue();

    [Fact] void should_not_end_metric_names_with_total() =>
        In("Metrics").Any(_ => _.Value.EndsWith("_total")).ShouldBeFalse();
}
