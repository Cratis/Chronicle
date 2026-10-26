// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// The kernel metric names, each followed by its UCUM unit.
/// </summary>
public static class WellKnownMetricNames
{
    /// <summary>Number of successful observations.</summary>
    public const string ObserverSuccessfulObservations = "cratis.chronicle.observer.successful_observations";

    /// <summary>Unit of <see cref="ObserverSuccessfulObservations"/>.</summary>
    public const string ObserverSuccessfulObservationsUnit = "{observation}";

    /// <summary>Number of failed handling attempts for partitions.</summary>
    public const string ObserverPartitionFailures = "cratis.chronicle.observer.partition_failures";

    /// <summary>Unit of <see cref="ObserverPartitionFailures"/>.</summary>
    public const string ObserverPartitionFailuresUnit = "{failure}";

    /// <summary>Number of partitions quarantined.</summary>
    public const string ObserverPartitionsQuarantined = "cratis.chronicle.observer.partitions_quarantined";

    /// <summary>Unit of <see cref="ObserverPartitionsQuarantined"/>.</summary>
    public const string ObserverPartitionsQuarantinedUnit = "{partition}";

    /// <summary>Number of partition quarantines cleared.</summary>
    public const string ObserverPartitionQuarantinesCleared = "cratis.chronicle.observer.partition_quarantines_cleared";

    /// <summary>Unit of <see cref="ObserverPartitionQuarantinesCleared"/>.</summary>
    public const string ObserverPartitionQuarantinesClearedUnit = "{partition}";

    /// <summary>Number of observer quarantines.</summary>
    public const string ObserverQuarantines = "cratis.chronicle.observer.quarantines";

    /// <summary>Unit of <see cref="ObserverQuarantines"/>.</summary>
    public const string ObserverQuarantinesUnit = "{quarantine}";

    /// <summary>Number of live events suspected skipped.</summary>
    public const string ObserverSuspectedSkippedEvents = "cratis.chronicle.observer.suspected_skipped_events";

    /// <summary>Unit of <see cref="ObserverSuspectedSkippedEvents"/>.</summary>
    public const string ObserverSuspectedSkippedEventsUnit = "{event}";

    /// <summary>Number of zero-work catch-up jobs.</summary>
    public const string ObserverZeroWorkCatchUps = "cratis.chronicle.observer.zero_work_catch_ups";

    /// <summary>Unit of <see cref="ObserverZeroWorkCatchUps"/>.</summary>
    public const string ObserverZeroWorkCatchUpsUnit = "{catch_up}";

    /// <summary>Number of shadow claim divergences.</summary>
    public const string ObserverClaimDivergences = "cratis.chronicle.observer.claim_divergences";

    /// <summary>Unit of <see cref="ObserverClaimDivergences"/>.</summary>
    public const string ObserverClaimDivergencesUnit = "{divergence}";

    /// <summary>Number of webhook probes scheduled.</summary>
    public const string WebhookProbesScheduled = "cratis.chronicle.webhook.probes_scheduled";

    /// <summary>Unit of <see cref="WebhookProbesScheduled"/>.</summary>
    public const string WebhookProbesScheduledUnit = "{probe}";

    /// <summary>Number of webhook probe attempts.</summary>
    public const string WebhookProbeAttempts = "cratis.chronicle.webhook.probe_attempts";

    /// <summary>Unit of <see cref="WebhookProbeAttempts"/>.</summary>
    public const string WebhookProbeAttemptsUnit = "{probe}";

    /// <summary>Number of failed webhook probes.</summary>
    public const string WebhookProbeFailures = "cratis.chronicle.webhook.probe_failures";

    /// <summary>Unit of <see cref="WebhookProbeFailures"/>.</summary>
    public const string WebhookProbeFailuresUnit = "{probe}";

    /// <summary>Number of successful webhook probes.</summary>
    public const string WebhookProbeSuccesses = "cratis.chronicle.webhook.probe_successes";

    /// <summary>Unit of <see cref="WebhookProbeSuccesses"/>.</summary>
    public const string WebhookProbeSuccessesUnit = "{probe}";

    /// <summary>Number of webhook probes that resumed an observer.</summary>
    public const string WebhookProbeResumes = "cratis.chronicle.webhook.probe_resumes";

    /// <summary>Unit of <see cref="WebhookProbeResumes"/>.</summary>
    public const string WebhookProbeResumesUnit = "{probe}";

    /// <summary>Number of webhook probes deferred.</summary>
    public const string WebhookProbesDeferred = "cratis.chronicle.webhook.probes_deferred";

    /// <summary>Unit of <see cref="WebhookProbesDeferred"/>.</summary>
    public const string WebhookProbesDeferredUnit = "{probe}";

    /// <summary>Number of webhook probes skipped.</summary>
    public const string WebhookProbesSkipped = "cratis.chronicle.webhook.probes_skipped";

    /// <summary>Unit of <see cref="WebhookProbesSkipped"/>.</summary>
    public const string WebhookProbesSkippedUnit = "{probe}";

    /// <summary>Number of webhook probes cancelled.</summary>
    public const string WebhookProbesCancelled = "cratis.chronicle.webhook.probes_cancelled";

    /// <summary>Unit of <see cref="WebhookProbesCancelled"/>.</summary>
    public const string WebhookProbesCancelledUnit = "{probe}";

    /// <summary>Number of webhook circuit openings.</summary>
    public const string WebhookCircuitOpenings = "cratis.chronicle.webhook.circuit_openings";

    /// <summary>Unit of <see cref="WebhookCircuitOpenings"/>.</summary>
    public const string WebhookCircuitOpeningsUnit = "{opening}";

    /// <summary>Number of webhook deliveries deferred by an open circuit.</summary>
    public const string WebhookCircuitRejections = "cratis.chronicle.webhook.circuit_rejections";

    /// <summary>Unit of <see cref="WebhookCircuitRejections"/>.</summary>
    public const string WebhookCircuitRejectionsUnit = "{delivery}";

    /// <summary>Number of events appended.</summary>
    public const string EventSequenceAppendedEvents = "cratis.chronicle.event_sequence.appended_events";

    /// <summary>Unit of <see cref="EventSequenceAppendedEvents"/>.</summary>
    public const string EventSequenceAppendedEventsUnit = "{event}";

    /// <summary>Number of duplicate event sequence numbers.</summary>
    public const string EventSequenceDuplicateSequenceNumbers = "cratis.chronicle.event_sequence.duplicate_sequence_numbers";

    /// <summary>Unit of <see cref="EventSequenceDuplicateSequenceNumbers"/>.</summary>
    public const string EventSequenceDuplicateSequenceNumbersUnit = "{event}";

    /// <summary>Number of events that failed to be appended.</summary>
    public const string EventSequenceAppendFailures = "cratis.chronicle.event_sequence.append_failures";

    /// <summary>Unit of <see cref="EventSequenceAppendFailures"/>.</summary>
    public const string EventSequenceAppendFailuresUnit = "{event}";

    /// <summary>Number of constraint violations.</summary>
    public const string EventSequenceConstraintViolations = "cratis.chronicle.event_sequence.constraint_violations";

    /// <summary>Unit of <see cref="EventSequenceConstraintViolations"/>.</summary>
    public const string EventSequenceConstraintViolationsUnit = "{violation}";

    /// <summary>Number of appends whose storage outcome is unknown.</summary>
    public const string EventSequenceUnknownAppendOutcomes = "cratis.chronicle.event_sequence.unknown_append_outcomes";

    /// <summary>Unit of <see cref="EventSequenceUnknownAppendOutcomes"/>.</summary>
    public const string EventSequenceUnknownAppendOutcomesUnit = "{append}";

    /// <summary>Number of failed constraint index updates.</summary>
    public const string ConstraintIndexUpdateFailures = "cratis.chronicle.constraint_index.update_failures";

    /// <summary>Unit of <see cref="ConstraintIndexUpdateFailures"/>.</summary>
    public const string ConstraintIndexUpdateFailuresUnit = "{failure}";

    /// <summary>Number of constraint index events settled with an error.</summary>
    public const string ConstraintIndexSettledErrors = "cratis.chronicle.constraint_index.settled_errors";

    /// <summary>Unit of <see cref="ConstraintIndexSettledErrors"/>.</summary>
    public const string ConstraintIndexSettledErrorsUnit = "{event}";

    /// <summary>Number of constraint index poison events.</summary>
    public const string ConstraintIndexPoisonEvents = "cratis.chronicle.constraint_index.poison_events";

    /// <summary>Unit of <see cref="ConstraintIndexPoisonEvents"/>.</summary>
    public const string ConstraintIndexPoisonEventsUnit = "{event}";

    /// <summary>Number of constraint index repairs.</summary>
    public const string ConstraintIndexRepairs = "cratis.chronicle.constraint_index.repairs";

    /// <summary>Unit of <see cref="ConstraintIndexRepairs"/>.</summary>
    public const string ConstraintIndexRepairsUnit = "{repair}";

    /// <summary>Number of appends refused for pending constraint indexes.</summary>
    public const string ConstraintIndexRefusals = "cratis.chronicle.constraint_index.refusals";

    /// <summary>Unit of <see cref="ConstraintIndexRefusals"/>.</summary>
    public const string ConstraintIndexRefusalsUnit = "{append}";

    /// <summary>Events not yet covered by confirmed constraint updates.</summary>
    public const string ConstraintIndexLag = "cratis.chronicle.constraint_index.lag";

    /// <summary>Unit of <see cref="ConstraintIndexLag"/>.</summary>
    public const string ConstraintIndexLagUnit = "{event}";

    /// <summary>Number of events enqueued.</summary>
    public const string AppendedEventsQueueEnqueuedEvents = "cratis.chronicle.appended_events_queue.enqueued_events";

    /// <summary>Unit of <see cref="AppendedEventsQueueEnqueuedEvents"/>.</summary>
    public const string AppendedEventsQueueEnqueuedEventsUnit = "{event}";

    /// <summary>Number of events handled.</summary>
    public const string AppendedEventsQueueHandledEvents = "cratis.chronicle.appended_events_queue.handled_events";

    /// <summary>Unit of <see cref="AppendedEventsQueueHandledEvents"/>.</summary>
    public const string AppendedEventsQueueHandledEventsUnit = "{event}";

    /// <summary>Number of events with handling failures.</summary>
    public const string AppendedEventsQueueHandlingFailures = "cratis.chronicle.appended_events_queue.handling_failures";

    /// <summary>Unit of <see cref="AppendedEventsQueueHandlingFailures"/>.</summary>
    public const string AppendedEventsQueueHandlingFailuresUnit = "{failure}";

    /// <summary>Number of events spilled to catch-up.</summary>
    public const string AppendedEventsQueueSpilledEvents = "cratis.chronicle.appended_events_queue.spilled_events";

    /// <summary>Unit of <see cref="AppendedEventsQueueSpilledEvents"/>.</summary>
    public const string AppendedEventsQueueSpilledEventsUnit = "{event}";

    /// <summary>Number of spilled catch-up triggers failing after the warning budget.</summary>
    public const string AppendedEventsQueueCatchUpTriggerBudgetExceeded = "cratis.chronicle.appended_events_queue.catch_up_trigger_budget_exceeded";

    /// <summary>Unit of <see cref="AppendedEventsQueueCatchUpTriggerBudgetExceeded"/>.</summary>
    public const string AppendedEventsQueueCatchUpTriggerBudgetExceededUnit = "{trigger}";

    /// <summary>Number of events suspected undelivered.</summary>
    public const string AppendedEventsQueueSuspectedUndeliveredEvents = "cratis.chronicle.appended_events_queue.suspected_undelivered_events";

    /// <summary>Unit of <see cref="AppendedEventsQueueSuspectedUndeliveredEvents"/>.</summary>
    public const string AppendedEventsQueueSuspectedUndeliveredEventsUnit = "{event}";

    /// <summary>Number of observers spilled to catch-up.</summary>
    public const string AppendedEventsQueueObserverSpills = "cratis.chronicle.appended_events_queue.observer_spills";

    /// <summary>Unit of <see cref="AppendedEventsQueueObserverSpills"/>.</summary>
    public const string AppendedEventsQueueObserverSpillsUnit = "{spill}";

    /// <summary>Number of failed alert transitions.</summary>
    public const string AlertFailedTransitions = "cratis.chronicle.alert.failed_transitions";

    /// <summary>Unit of <see cref="AlertFailedTransitions"/>.</summary>
    public const string AlertFailedTransitionsUnit = "{transition}";

    /// <summary>Number of open alert incidents.</summary>
    public const string AlertOpenIncidents = "cratis.chronicle.alert.open_incidents";

    /// <summary>Unit of <see cref="AlertOpenIncidents"/>.</summary>
    public const string AlertOpenIncidentsUnit = "{incident}";

    /// <summary>Whether the open incident count is available.</summary>
    public const string AlertOpenIncidentsAvailable = "cratis.chronicle.alert.open_incidents_available";

    /// <summary>Unit of <see cref="AlertOpenIncidentsAvailable"/>.</summary>
    public const string AlertOpenIncidentsAvailableUnit = "1";

    /// <summary>Number of late results for abandoned deliveries.</summary>
    public const string AbandonedDeliveryLateResults = "cratis.chronicle.abandoned_delivery.late_results";

    /// <summary>Unit of <see cref="AbandonedDeliveryLateResults"/>.</summary>
    public const string AbandonedDeliveryLateResultsUnit = "{result}";

    /// <summary>Number of abandoned delivery hold expirations.</summary>
    public const string AbandonedDeliveryHoldExpirations = "cratis.chronicle.abandoned_delivery.hold_expirations";

    /// <summary>Unit of <see cref="AbandonedDeliveryHoldExpirations"/>.</summary>
    public const string AbandonedDeliveryHoldExpirationsUnit = "{delivery}";

    /// <summary>Time spent fencing abandoned deliveries.</summary>
    public const string AbandonedDeliveryFenceDuration = "cratis.chronicle.abandoned_delivery.fence_duration";

    /// <summary>Unit of <see cref="AbandonedDeliveryFenceDuration"/>.</summary>
    public const string AbandonedDeliveryFenceDurationUnit = "s";

    /// <summary>Number of connected clients.</summary>
    public const string ConnectedClientCount = "cratis.chronicle.connected_client.count";

    /// <summary>Unit of <see cref="ConnectedClientCount"/>.</summary>
    public const string ConnectedClientCountUnit = "{client}";

    /// <summary>Time spent waiting for a database schema lock.</summary>
    public const string SqlSchemaLockWaitDuration = "cratis.chronicle.sql.schema_lock.wait_duration";

    /// <summary>Unit of <see cref="SqlSchemaLockWaitDuration"/>.</summary>
    public const string SqlSchemaLockWaitDurationUnit = "s";

    /// <summary>Number of failed SQL live-query polls.</summary>
    public const string SqlLiveQueryPollFailures = "cratis.chronicle.sql.live_query.poll_failures";

    /// <summary>Unit of <see cref="SqlLiveQueryPollFailures"/>.</summary>
    public const string SqlLiveQueryPollFailuresUnit = "{poll}";

    /// <summary>Number of concurrent SQL insert collisions recovered.</summary>
    public const string SqlUpsertRecoveredCollisions = "cratis.chronicle.sql.upsert.recovered_collisions";

    /// <summary>Unit of <see cref="SqlUpsertRecoveredCollisions"/>.</summary>
    public const string SqlUpsertRecoveredCollisionsUnit = "{collision}";

    /// <summary>Number of failed composite key stamps.</summary>
    public const string SqlCompositeKeyStampFailures = "cratis.chronicle.sql.composite_key.stamp_failures";

    /// <summary>Unit of <see cref="SqlCompositeKeyStampFailures"/>.</summary>
    public const string SqlCompositeKeyStampFailuresUnit = "{failure}";

    /// <summary>Number of composite key collisions.</summary>
    public const string SqlCompositeKeyCollisions = "cratis.chronicle.sql.composite_key.collisions";

    /// <summary>Unit of <see cref="SqlCompositeKeyCollisions"/>.</summary>
    public const string SqlCompositeKeyCollisionsUnit = "{collision}";
}
