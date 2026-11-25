// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_the_subscription_changes_during_a_gap_read : given.a_queue_with_a_paused_gap_read
{
    void Establish() => _history = [MatchingAt(102)];

    async Task Because()
    {
        await _queue.Enqueue([Doorbell()]);
        await _readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _queue.Subscribe(_observerKey, [_eventType], new(["replacement"]));
        _resumeRead.TrySetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_recheck_the_replacement_subscription() => _observer.Received(2).GetState();
    [Fact] void should_not_drop_the_live_batch_because_of_an_obsolete_gap() => _handledEvents.ShouldEqual(1);
    [Fact] void should_not_spill_the_replacement_subscription() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_not_warn_about_the_obsolete_gap() => LiveStartGapWarnings.ShouldBeEmpty();
}
