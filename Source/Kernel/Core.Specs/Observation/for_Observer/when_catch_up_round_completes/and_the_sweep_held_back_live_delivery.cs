// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// Once the sweep reports back, the observer routes from the position the sweep reached and delivers what is
/// appended after it live again (#5786).
/// </summary>
public class and_the_sweep_held_back_live_delivery : given.an_observer_with_a_page_subscriber_catching_up
{
    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(13UL));
    }

    async Task Because()
    {
        await _observer.Handle(PartitionAppendedAfterEnumeration, [EventAt(12)]);
        await _observer.CaughtUp(_job, 12UL, 12UL);
        await _observer.Handle(PartitionAppendedAfterEnumeration, [EventAt(13)]);
    }

    [Fact] async Task should_stop_holding_back_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_not_deliver_the_event_the_sweep_covered_live() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Any(e => e.Context.SequenceNumber == 12UL)), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_deliver_the_event_appended_after_the_sweep_live() => _subscriber.Received(1).OnNext(PartitionAppendedAfterEnumeration, Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Single().Context.SequenceNumber == 13UL), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_past_the_event_appended_after_the_sweep() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
}
