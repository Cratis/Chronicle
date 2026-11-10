// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_spilling_committed_events;

public class and_a_subscriber_observes_their_event_type : given.a_single_subscriber_with_an_event_type
{
    readonly TaskCompletionSource _caughtUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _afterSpill;

    void Establish() => _observer.CatchUp().Returns(_ =>
    {
        _caughtUp.TrySetResult();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        await _queue.SpillMatchingToCatchup([Event(_eventType, 7)]);
        _afterSpill = await _queue.GetSubscriptions();
        await _caughtUp.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _queue.Enqueue([Event(_eventType, 8)]);
        await _queue.AwaitQueueDepletion();
    }

    void Destroy() => _queue.Dispose();

    [Fact] void should_take_it_out_of_live_delivery() => _afterSpill.ShouldBeEmpty();
    [Fact] async Task should_hand_it_to_its_catch_up_once() => await _observer.Received(1).CatchUp();
    [Fact] async Task should_not_deliver_the_committed_events_live() => await _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Any(e => e.Context.SequenceNumber == 7UL)));
    [Fact] async Task should_not_deliver_an_interleaved_append_that_would_carry_its_cursor_past_them() => await _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());

    static AppendedEvent Event(EventType eventType, ulong sequenceNumber) =>
        AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = eventType, EventSourceId = "source", SequenceNumber = sequenceNumber } };
}
