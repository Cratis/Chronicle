// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal sealed class AppendedEventsQueueMetrics(MetricSet metrics) : IDisposable
{
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _enqueued = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueEnqueuedEvents, WellKnownMetricNames.AppendedEventsQueueEnqueuedEventsUnit, "Number of events enqueued");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _handled = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueHandledEvents, WellKnownMetricNames.AppendedEventsQueueHandledEventsUnit, "Number of events processed");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _failures = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueHandlingFailures, WellKnownMetricNames.AppendedEventsQueueHandlingFailuresUnit, "Number of events that has handling failures");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _spilled = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueSpilledEvents, WellKnownMetricNames.AppendedEventsQueueSpilledEventsUnit, "Number of events spilled to observer catch-up when the queue was full");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _budgetExceeded = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueCatchUpTriggerBudgetExceeded, WellKnownMetricNames.AppendedEventsQueueCatchUpTriggerBudgetExceededUnit, "Number of spilled observer catch-up triggers still failing after the warning budget");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _undelivered = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueSuspectedUndeliveredEvents, WellKnownMetricNames.AppendedEventsQueueSuspectedUndeliveredEventsUnit, "Number of event deliveries to an observer whose dispatch failed; one event can count once per observer", WellKnownTelemetryAttributes.ObserverId);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _observersSpilled = metrics.Counter<int>(WellKnownMetricNames.AppendedEventsQueueObserverSpills, WellKnownMetricNames.AppendedEventsQueueObserverSpillsUnit, "Number of times an observer was moved from live delivery to catch-up, by reason", WellKnownTelemetryAttributes.ObserverId, WellKnownTelemetryAttributes.ObserverSpillReason);

    public void Dispose()
    {
        // Instruments belong to the injected meter, not to a queue.
    }

    internal void EventsEnqueued(int numberOfEvents) => _enqueued.Record(numberOfEvents);
    internal void EventsHandled(int numberOfEvents) => _handled.Record(numberOfEvents);
    internal void EventsHandlingFailures() => _failures.Record(1);
    internal void EventsSpilledToCatchup(int numberOfEvents) => _spilled.Record(numberOfEvents);
    internal void CatchupTriggerBudgetExceeded() => _budgetExceeded.Record(1);

    internal void EventsSuspectedUndelivered(int numberOfEvents, ObserverId observerId) => _undelivered.Record(numberOfEvents, observerId);
    internal void ObserverSpilledToCatchup(ObserverId observerId, string reason) => _observersSpilled.Record(1, observerId, reason);
}
