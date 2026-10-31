// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// Unsubscribing drops what the queue had routed to the observer but not yet delivered; only the delivery already in
/// flight completes.
/// </summary>
public class and_it_unsubscribes : given.a_blocked_observer_and_a_free_observer
{
    const int Batches = 4;

    async Task Because()
    {
        for (var batch = 0; batch < Batches; batch++)
        {
            await _queue.Enqueue([EventFor("partition")]);
        }

        await WaitTill(() => FreeObserverEventCount == Batches && !_blockedObserverHandled.IsEmpty);
        await _queue.Unsubscribe(_blockedObserverKey);
        _release.SetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_only_complete_the_delivery_in_flight() => _blockedObserverHandled.Count.ShouldEqual(1);
    [Fact] void should_deliver_every_event_to_the_other_observer() => FreeObserverEventCount.ShouldEqual(Batches);
}
