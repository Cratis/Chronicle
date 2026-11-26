// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Repair of the first crossed event must also keep later held-back events pending.
/// </summary>
public class and_held_back_events_span_next : given.an_observer_with_skip_detection
{
    int[] _measurementsAfterFirstRound;

    async Task Establish()
    {
        PersistStartedPartitionCatchUps();
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
        await _silo.RunCatchUpContinuation();
        _measurementsAfterFirstRound = _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).ToArray();
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 19UL)]);
    }

    [Fact] void should_only_report_the_crossed_event_first() => _measurementsAfterFirstRound.SequenceEqual([1]).ShouldBeTrue();
    [Fact] void should_not_cross_the_other_event_while_repair_owns_it() => _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).SequenceEqual([1]).ShouldBeTrue();
    [Fact] void should_keep_the_later_event_pending() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"Q");
    [Fact] void should_keep_the_accepted_repair_range_durable() => _stateStorage.State.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("Q", 10UL, 13UL));
    [Fact] void should_transfer_the_suffix_to_the_unbounded_repair() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"Q" && _.FromSequenceNumber == (EventSequenceNumber)10UL && _.ToSequenceNumber == EventSequenceNumber.Max && _.IsHandoverRepair));
}
