// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_projection_catch_up_round_completes;

/// <summary>
/// A step reporting too little coverage must retain pending work rather than advance past its held-back event.
/// </summary>
public class and_the_step_delivered_but_reported_a_lower_position : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        PersistStartedPartitionCatchUps();
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["A"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(12UL));
    }

    async Task Because()
    {
        await _observer.Handle("A", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);
        await _observer.CaughtUp(JobId.NotSet, 9UL, 9UL);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_not_measure_a_skip_that_was_prevented() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn_for_a_skip_that_was_prevented() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
    [Fact] void should_keep_the_unproven_delivery_pending() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"A");
}
