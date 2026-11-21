// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_the_round_reported_held_back_partitions_in_a_batch : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["A", "B", "C"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
    }

    async Task Because()
    {
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("C", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)]);
        var step = JobStepId.New();
        await _observer.ReportHandledEventsBatch(step,
        [
            new("B", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1 }, step, 10UL),
            new("C", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1 }, step, 11UL)
        ]);
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
    }

    [Fact] void should_not_start_repairs_for_reported_deliveries() => CheckDidNotStartCatchupJob();
    [Fact] void should_release_the_reported_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_advance_past_the_reported_events() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
