// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;
using Cratis.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class EventSequenceMetrics
{
    [Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, "Number of events appended to the event sequence", WellKnownMetricNames.EventSequenceAppendedEventsUnit)]
    internal static partial void AppendedEvent(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryAttributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownMetricNames.EventSequenceDuplicateSequenceNumbers, "Number of duplicate event sequence numbers", WellKnownMetricNames.EventSequenceDuplicateSequenceNumbersUnit)]
    internal static partial void DuplicateEventSequenceNumber(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryAttributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownMetricNames.EventSequenceAppendFailures, "Number of events that failed to be appended to the event sequence", WellKnownMetricNames.EventSequenceAppendFailuresUnit)]
    internal static partial void FailedAppending(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryAttributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownMetricNames.EventSequenceConstraintViolations, "Number of constraint violations", WellKnownMetricNames.EventSequenceConstraintViolationsUnit)]
    internal static partial void ConstraintViolation(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryAttributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownMetricNames.ConstraintIndexUpdateFailures, "Number of failed constraint index attempts", WellKnownMetricNames.ConstraintIndexUpdateFailuresUnit)]
    internal static partial void ConstraintIndexUpdateFailed(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownMetricNames.ConstraintIndexSettledErrors, "Number of constraint index events settled with a deterministic error", WellKnownMetricNames.ConstraintIndexSettledErrorsUnit)]
    internal static partial void ConstraintIndexSettledWithError(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownMetricNames.ConstraintIndexPoisonEvents, "Number of constraint event effects settled after five identical non-transient failures", WellKnownMetricNames.ConstraintIndexPoisonEventsUnit)]
    internal static partial void ConstraintIndexPoisonEvent(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownMetricNames.ConstraintIndexRepairs, "Number of completed constraint index repairs", WellKnownMetricNames.ConstraintIndexRepairsUnit)]
    internal static partial void ConstraintIndexRepaired(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownMetricNames.ConstraintIndexRefusals, "Number of appends refused for pending constraint indexes", WellKnownMetricNames.ConstraintIndexRefusalsUnit)]
    internal static partial void ConstraintIndexRefused(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownMetricNames.EventSequenceUnknownAppendOutcomes, "Number of appends whose storage outcome could not be settled", WellKnownMetricNames.EventSequenceUnknownAppendOutcomesUnit)]
    internal static partial void AppendOutcomeUnknown(this IMeterScope<EventSequence> scope);

    [Gauge<long>(WellKnownMetricNames.ConstraintIndexLag, "Events not yet covered by confirmed constraint updates", WellKnownMetricNames.ConstraintIndexLagUnit)]
    internal static partial void ConstraintIndexLag(this IMeterScope<EventSequence> scope, long lag);
}

internal static class EventSequenceMetricsScopes
{
    internal static IMeterScope<EventSequence> BeginEventSequenceScope(this IMeter<EventSequence> meter, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            [WellKnownTelemetryAttributes.EventStoreName] = eventStore,
            [WellKnownTelemetryAttributes.EventStoreNamespace] = @namespace
        });
}
