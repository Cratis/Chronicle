// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// An observer waiting on a slow downstream call must only hold back itself. The other observer on the queue gets
/// every event, in sequence order and across interleaved partitions, while the first is still on its first delivery.
/// </summary>
public class and_another_observer_shares_the_queue : given.a_blocked_observer_and_a_free_observer
{
    const int Batches = 12;
    readonly EventSourceId[] _partitions = ["first", "second", "third"];
    int _freeObserverEventsWhileBlocked;
    int _blockedObserverDeliveriesWhileBlocked;
    IEnumerable<EventSequenceNumber> _freeObserverSequenceNumbers;

    async Task Because()
    {
        for (var batch = 0; batch < Batches; batch++)
        {
            await _queue.Enqueue([EventFor(_partitions[batch % _partitions.Length])]);
        }

        await WaitTill(() => FreeObserverEventCount == Batches);
        _freeObserverEventsWhileBlocked = FreeObserverEventCount;
        _blockedObserverDeliveriesWhileBlocked = _blockedObserverHandled.Count;
        _freeObserverSequenceNumbers = _freeObserverHandled.SelectMany(handled => handled.Events).Select(@event => @event.Context.SequenceNumber).ToArray();

        _release.SetResult();
        await WaitTill(() => _blockedObserverHandled.Sum(handled => handled.Events.Count()) == Batches);
    }

    [Fact] void should_deliver_every_event_to_the_free_observer_while_the_other_is_blocked() => _freeObserverEventsWhileBlocked.ShouldEqual(Batches);
    [Fact] void should_have_the_blocked_observer_still_on_its_first_delivery() => _blockedObserverDeliveriesWhileBlocked.ShouldEqual(1);
    [Fact] void should_deliver_to_the_free_observer_in_sequence_order() => _freeObserverSequenceNumbers.ShouldEqual(_freeObserverSequenceNumbers.OrderBy(_ => _.Value).ToArray());
    [Fact] void should_deliver_every_event_to_the_blocked_observer_once_released() => _blockedObserverHandled.Sum(handled => handled.Events.Count()).ShouldEqual(Batches);
    [Fact] void should_deliver_to_the_blocked_observer_in_sequence_order() => _blockedObserverHandled.SelectMany(handled => handled.Events).Select(@event => @event.Context.SequenceNumber.Value).ShouldEqual(_blockedObserverHandled.SelectMany(handled => handled.Events).Select(@event => @event.Context.SequenceNumber.Value).Order().ToArray());
}
