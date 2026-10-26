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
    [Counter<int>(WellKnownTelemetryNames.Metrics.EventSequenceAppendedEvents, "Number of events appended to the event sequence", WellKnownTelemetryNames.Metrics.EventSequenceAppendedEventsUnit)]
    internal static partial void AppendedEvent(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryNames.Attributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownTelemetryNames.Metrics.EventSequenceDuplicateSequenceNumbers, "Number of duplicate event sequence numbers", WellKnownTelemetryNames.Metrics.EventSequenceDuplicateSequenceNumbersUnit)]
    internal static partial void DuplicateEventSequenceNumber(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryNames.Attributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownTelemetryNames.Metrics.EventSequenceAppendFailures, "Number of events that failed to be appended to the event sequence", WellKnownTelemetryNames.Metrics.EventSequenceAppendFailuresUnit)]
    internal static partial void FailedAppending(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryNames.Attributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownTelemetryNames.Metrics.EventSequenceConstraintViolations, "Number of constraint violations", WellKnownTelemetryNames.Metrics.EventSequenceConstraintViolationsUnit)]
    internal static partial void ConstraintViolation(this IMeterScope<EventSequence> scope, [Tag(WellKnownTelemetryNames.Attributes.EventTypeId)] string eventName);

    [Counter<int>(WellKnownTelemetryNames.Metrics.ConstraintIndexUpdateFailures, "Number of failed constraint index attempts", WellKnownTelemetryNames.Metrics.ConstraintIndexUpdateFailuresUnit)]
    internal static partial void ConstraintIndexUpdateFailed(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownTelemetryNames.Metrics.ConstraintIndexSettledErrors, "Number of constraint index events settled with a deterministic error", WellKnownTelemetryNames.Metrics.ConstraintIndexSettledErrorsUnit)]
    internal static partial void ConstraintIndexSettledWithError(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownTelemetryNames.Metrics.ConstraintIndexPoisonEvents, "Number of constraint event effects settled after five identical non-transient failures", WellKnownTelemetryNames.Metrics.ConstraintIndexPoisonEventsUnit)]
    internal static partial void ConstraintIndexPoisonEvent(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownTelemetryNames.Metrics.ConstraintIndexRepairs, "Number of completed constraint index repairs", WellKnownTelemetryNames.Metrics.ConstraintIndexRepairsUnit)]
    internal static partial void ConstraintIndexRepaired(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownTelemetryNames.Metrics.ConstraintIndexRefusals, "Number of appends refused for pending constraint indexes", WellKnownTelemetryNames.Metrics.ConstraintIndexRefusalsUnit)]
    internal static partial void ConstraintIndexRefused(this IMeterScope<EventSequence> scope);

    [Counter<int>(WellKnownTelemetryNames.Metrics.EventSequenceUnknownAppendOutcomes, "Number of appends whose storage outcome could not be settled", WellKnownTelemetryNames.Metrics.EventSequenceUnknownAppendOutcomesUnit)]
    internal static partial void AppendOutcomeUnknown(this IMeterScope<EventSequence> scope);

    [Gauge<long>(WellKnownTelemetryNames.Metrics.ConstraintIndexLag, "Events not yet covered by confirmed constraint updates", WellKnownTelemetryNames.Metrics.ConstraintIndexLagUnit)]
    internal static partial void ConstraintIndexLag(this IMeterScope<EventSequence> scope, long lag);
}

internal static class EventSequenceMetricsScopes
{
    internal static IMeterScope<EventSequence> BeginEventSequenceScope(this IMeter<EventSequence> meter, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            [WellKnownTelemetryNames.Attributes.EventStoreName] = eventStore,
            [WellKnownTelemetryNames.Attributes.EventStoreNamespace] = @namespace
        });
}
