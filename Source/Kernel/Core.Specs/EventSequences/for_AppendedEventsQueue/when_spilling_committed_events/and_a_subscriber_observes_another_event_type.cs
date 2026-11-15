// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_spilling_committed_events;

public class and_a_subscriber_observes_another_event_type : given.a_single_subscriber_with_an_event_type
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _afterSpill;

    async Task Because()
    {
        var imported = AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = new EventType("imported-type", 1), EventSourceId = "source", SequenceNumber = 7 } };
        await _queue.SpillMatchingToCatchup([imported]);
        _afterSpill = await _queue.GetSubscriptions();
    }

    void Destroy() => _queue.Dispose();

    [Fact] void should_keep_it_live_so_it_does_not_look_unsubscribed() => _afterSpill.Select(_ => _.ObserverKey).ShouldContainOnly(_observerKey);
    [Fact] async Task should_not_catch_it_up() => await _observer.DidNotReceive().CatchUp();
}
