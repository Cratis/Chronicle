// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// When more was appended during the sweep than the observer keeps, the events above what the sweep read are no longer
/// all accounted for, so nothing is handed over and the observer catches up again from where the sweep stopped.
/// </summary>
public class and_more_was_held_back_than_is_kept : given.an_observer_with_a_page_subscriber_catching_up
{
    const ulong LastAppended = 13UL + Observer.MaximumEventsHeldBackDuringSweep;

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)LastAppended);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)13UL);
    }

    async Task Because()
    {
        var aboveWhatTheSweepRead = Enumerable.Range(13, Observer.MaximumEventsHeldBackDuringSweep).Select(_ => EventAt((ulong)_)).ToArray();
        await _observer.Handle(RegisteredPartition, aboveWhatTheSweepRead);
        await _observer.Handle(PartitionAppendedAfterEnumeration, [EventAt(LastAppended)]);
        await _observer.CaughtUp(_job, 12UL, 12UL);
    }

    [Fact] void should_not_hand_anything_over() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_catch_up_again_from_where_the_sweep_stopped() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
}
