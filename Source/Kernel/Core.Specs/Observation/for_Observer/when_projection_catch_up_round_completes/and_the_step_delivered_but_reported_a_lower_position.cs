// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_projection_catch_up_round_completes;

/// <summary>
/// A step that delivered an event but reported a lower position cannot be told from a skipped event, so it is
/// suspected - once, and never as anything stronger.
/// </summary>
public class and_the_step_delivered_but_reported_a_lower_position : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
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
    }

    [Fact] void should_measure_exactly_one_suspected_skip() => _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).SequenceEqual([1]).ShouldBeTrue();
    [Fact] void should_warn_once_that_it_is_only_a_suspicion() => _recordedLogs.SuspectedSkipWarnings.Single().ShouldContain("suspicion, not proof");
    [Fact] void should_name_the_partition_and_the_sequence_number() => _recordedLogs.SuspectedSkipWarnings.Single().ShouldContain("partition A, first at sequence number 10");
}
