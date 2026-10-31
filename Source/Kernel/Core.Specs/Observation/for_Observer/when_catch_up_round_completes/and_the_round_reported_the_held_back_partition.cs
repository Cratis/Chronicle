// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The round delivered the held back partition's events and said so, so nothing is suspected.
/// </summary>
public class and_the_round_reported_the_held_back_partition : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["A", "B"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
    }

    async Task Because()
    {
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.ReportHandledEvents("B", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1 }, JobStepId.New(), 1UL);
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
    }

    [Fact] void should_advance_past_the_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
