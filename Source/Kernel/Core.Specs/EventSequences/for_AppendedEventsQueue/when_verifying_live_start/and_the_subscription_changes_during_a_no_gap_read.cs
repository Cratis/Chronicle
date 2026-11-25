// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_the_subscription_changes_during_a_no_gap_read : given.a_queue_with_a_paused_gap_read
{
    async Task Because()
    {
        await _queue.Enqueue([Doorbell()]);
        await _readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        var missing = MatchingAt(103);
        _history = [missing with { Context = missing.Context with { Tags = [new("replacement")] } }];
        await _queue.Subscribe(_observerKey, [_eventType], new(["replacement"]));
        _resumeRead.TrySetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_recheck_the_current_subscription_without_another_routed_batch() => _observer.Received(2).GetState();
    [Fact] void should_not_cache_the_old_filters_no_gap_result() => _handledEvents.ShouldEqual(0);
    [Fact] void should_recover_the_new_subscriptions_matching_gap() => _observer.Received(1).CatchUp();
}
