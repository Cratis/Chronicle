// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Events held back on both sides of the position are reported one at a time, as the position crosses each of them.
/// </summary>
public class and_held_back_events_span_next : given.an_observer_with_skip_detection
{
    int[] _measurementsAfterFirstRound;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["Q"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _subscriber.OnNext((Key)"B", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(12UL), ObserverSubscriberResult.Ok(20UL));
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 20UL)]);
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        _measurementsAfterFirstRound = _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).ToArray();
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 19UL)]);
    }

    [Fact] void should_only_report_the_crossed_event_first() => _measurementsAfterFirstRound.SequenceEqual([1]).ShouldBeTrue();
    [Fact] void should_report_the_other_when_it_is_crossed() => _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).SequenceEqual([1, 1]).ShouldBeTrue();
}
