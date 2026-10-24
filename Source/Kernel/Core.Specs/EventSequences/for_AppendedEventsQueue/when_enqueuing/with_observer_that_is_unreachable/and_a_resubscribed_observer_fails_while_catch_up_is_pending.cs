// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_a_resubscribed_observer_fails_while_catch_up_is_pending : given.an_unreachable_observer
{
    void Establish() => _observer.CatchUp().Returns(Task.FromException(new InvalidOperationException("catch-up unreachable")));

    async Task Because()
    {
        await DispatchFirstEvent();
        await _clock.WaitForNextDelay();
        await _queue.Subscribe(_observerKey, [_eventType]);
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    [Fact] void should_reuse_the_pending_trigger() => _observer.Received(1).CatchUp();
    [Fact] async Task should_also_spill_the_failed_replacement_subscription() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
