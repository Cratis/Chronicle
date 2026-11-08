// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_holding_back;

public class and_a_subscriber_observes_the_held_event_type : given.a_single_subscriber_with_an_event_type
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _whileHeld;

    void Establish() => _observer.CatchUp().Returns(Task.CompletedTask);

    async Task Because()
    {
        await _queue.HoldBack("import-1", [_eventType.Id]);
        _whileHeld = await _queue.GetSubscriptions();
        await _queue.Enqueue([AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "source", SequenceNumber = 7 } }]);
        await _queue.AwaitQueueDepletion();
        await _queue.ReleaseHold("import-1");
        await Task.Delay(100);
    }

    [Fact] void should_take_it_out_of_live_delivery_while_held() => _whileHeld.ShouldBeEmpty();
    [Fact] async Task should_not_deliver_an_interleaved_append_that_would_carry_its_cursor_past_the_import() => await _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] async Task should_catch_it_up_once_when_released() => await _observer.Received(1).CatchUp();
}
