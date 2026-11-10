// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// Events appended while the sweep runs are held back from live delivery; those above what the sweep read are handed
/// to the subscriber when it reports back, rather than left to another sweep.
/// </summary>
public class and_the_sweep_held_back_events_above_what_it_read : given.an_observer_with_a_page_subscriber_catching_up
{
    void Establish() =>
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber.Value));

    async Task Because()
    {
        await _observer.Handle(PartitionAppendedAfterEnumeration, [EventAt(12)]);
        await _observer.Handle(RegisteredPartition, [EventAt(13)]);
        await _observer.CaughtUp(_job, 12UL, 12UL);
    }

    [Fact] void should_not_deliver_the_event_the_sweep_read() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Any(e => e.Context.SequenceNumber == 12UL)), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_deliver_the_event_above_what_the_sweep_read_once() => _subscriber.Received(1).OnNext(RegisteredPartition, Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Single().Context.SequenceNumber == 13UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_past_the_handed_over_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] async Task should_stop_holding_back_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_start_another_catch_up() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
