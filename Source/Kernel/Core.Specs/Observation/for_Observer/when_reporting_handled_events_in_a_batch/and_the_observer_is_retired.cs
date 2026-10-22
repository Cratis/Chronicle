// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_handled_events_in_a_batch;

public class and_the_observer_is_retired : given.an_observer_with_skip_detection
{
    EventSkipDetector _skips;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["first", "second"]);
        await _observer.Handle("first", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("second", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)]);
        await _observer.Retire();
        _skips = (EventSkipDetector)typeof(Observer).GetField("_skipDetector", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_observer);
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        var step = JobStepId.New();
        await _observer.ReportHandledEventsBatch(step,
        [
            new("first", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1UL }, step, 10UL),
            new("second", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1UL }, step, 11UL)
        ]);

        _stateStorage.State.CatchingUpPartitions.Clear();
        _skips.Evaluate(13UL);
    }

    [Fact] void should_not_suspect_the_reported_partitions() => _suspectedSkips.ShouldEqual(0);
    [Fact] void should_not_warn_about_reported_events() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
    [Fact] void should_not_persist_the_retired_observers_counts() => _observerHandledCountsStorage.DidNotReceive().Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
}
