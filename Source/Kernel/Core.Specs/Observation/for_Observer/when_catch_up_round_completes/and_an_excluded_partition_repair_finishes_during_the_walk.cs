// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_an_excluded_partition_repair_finishes_during_the_walk : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _observer.SetSubscription(subscription with { SubscriberType = typeof(IObserverPartitionPageSubscriber) });
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 13UL,
            PendingCatchUpRepairs = [new PendingCatchUpRepair("P", 10UL, 13UL)],
            CatchingUpPartitions = new HashSet<Key> { "P" }
        };
        await _observer.RegisterCatchingUpPartitions(["Q"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)16UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        PersistStartedPartitionCatchUps();
    }

    async Task Because()
    {
        await _observer.PartitionCaughtUp("P", 13UL);
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 14UL)]);
        await _observer.CaughtUp(JobId.NotSet, 16UL, 16UL);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_not_deliver_the_excluded_event_out_of_order() => _subscriber.DidNotReceive()
        .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_repair_the_event_outside_the_finished_owners_range() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ =>
            _.Key == (Key)"P" && _.FromSequenceNumber == (EventSequenceNumber)14UL && _.ToSequenceNumber == EventSequenceNumber.Max));
    [Fact] void should_retain_the_durable_uncovered_suffix() => _stateStorage.State.PendingCatchUpRepairs!
        .ShouldContainOnly(new PendingCatchUpRepair("P", 14UL, 17UL));
}
