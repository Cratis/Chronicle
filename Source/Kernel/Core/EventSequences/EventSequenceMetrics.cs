// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Instruments, scope tags and additional tag keys selected once at activation, never during append.
/// </summary>
/// <param name="metrics">The preselected metric set.</param>
internal sealed class EventSequenceMetrics(MetricSet metrics) : IDisposable
{
    readonly IMetricInstrument<long> _rebuilding = metrics.Gauge<long>(WellKnownMetricNames.ConstraintIndexesRebuilding, WellKnownMetricNames.ConstraintIndexesRebuildingUnit, "Constraints whose index is held as rebuilding and refuses governed appends");
    readonly IMetricInstrument<int> _rebuildEnds = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexRebuildEnds, WellKnownMetricNames.ConstraintIndexRebuildEndsUnit, "Ended constraint index rebuilds by handoff reason", "reason");
    readonly IMetricInstrument<int> _appended = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendedEvents, WellKnownMetricNames.EventSequenceAppendedEventsUnit, "Number of events appended to the event sequence", WellKnownTelemetryAttributes.EventTypeId);
    readonly IMetricInstrument<int> _duplicates = metrics.Counter<int>(WellKnownMetricNames.EventSequenceDuplicateSequenceNumbers, WellKnownMetricNames.EventSequenceDuplicateSequenceNumbersUnit, "Number of duplicate event sequence numbers", WellKnownTelemetryAttributes.EventTypeId);
    readonly IMetricInstrument<int> _failed = metrics.Counter<int>(WellKnownMetricNames.EventSequenceAppendFailures, WellKnownMetricNames.EventSequenceAppendFailuresUnit, "Number of events that failed to be appended to the event sequence", WellKnownTelemetryAttributes.EventTypeId);
    readonly IMetricInstrument<int> _violations = metrics.Counter<int>(WellKnownMetricNames.EventSequenceConstraintViolations, WellKnownMetricNames.EventSequenceConstraintViolationsUnit, "Number of constraint violations", WellKnownTelemetryAttributes.EventTypeId);
    readonly IMetricInstrument<int> _updateFailures = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexUpdateFailures, WellKnownMetricNames.ConstraintIndexUpdateFailuresUnit, "Number of failed constraint index attempts");
    readonly IMetricInstrument<int> _settledErrors = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexSettledErrors, WellKnownMetricNames.ConstraintIndexSettledErrorsUnit, "Number of constraint index events settled with a deterministic error");
    readonly IMetricInstrument<int> _poison = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexPoisonEvents, WellKnownMetricNames.ConstraintIndexPoisonEventsUnit, "Number of constraint event effects settled after five identical non-transient failures");
    readonly IMetricInstrument<int> _repairs = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexRepairs, WellKnownMetricNames.ConstraintIndexRepairsUnit, "Number of completed constraint index repairs");
    readonly IMetricInstrument<int> _refusals = metrics.Counter<int>(WellKnownMetricNames.ConstraintIndexRefusals, WellKnownMetricNames.ConstraintIndexRefusalsUnit, "Number of appends refused for pending constraint indexes");
    readonly IMetricInstrument<int> _unknown = metrics.Counter<int>(WellKnownMetricNames.EventSequenceUnknownAppendOutcomes, WellKnownMetricNames.EventSequenceUnknownAppendOutcomesUnit, "Number of appends whose storage outcome could not be settled");
    readonly IMetricInstrument<long> _lag = metrics.Gauge<long>(WellKnownMetricNames.ConstraintIndexLag, WellKnownMetricNames.ConstraintIndexLagUnit, "Events not yet covered by confirmed constraint updates");

    public void Dispose()
    {
        // Instruments belong to the injected meter, not to an activation.
    }

    internal void ConstraintIndexesRebuilding(long count) => _rebuilding.Record(count);
    internal void ConstraintIndexRebuildEnded(string reason) => _rebuildEnds.Record(1, reason);
    internal void AppendedEvent(string eventName) => _appended.Record(1, eventName);
    internal void DuplicateEventSequenceNumber(string eventName) => _duplicates.Record(1, eventName);
    internal void FailedAppending(string eventName) => _failed.Record(1, eventName);
    internal void ConstraintViolation(string eventName) => _violations.Record(1, eventName);
    internal void ConstraintIndexUpdateFailed() => _updateFailures.Record(1);
    internal void ConstraintIndexSettledWithError() => _settledErrors.Record(1);
    internal void ConstraintIndexPoisonEvent() => _poison.Record(1);
    internal void ConstraintIndexRepaired() => _repairs.Record(1);
    internal void ConstraintIndexRefused() => _refusals.Record(1);
    internal void AppendOutcomeUnknown() => _unknown.Record(1);
    internal void ConstraintIndexLag(long lag) => _lag.Record(lag);
}
