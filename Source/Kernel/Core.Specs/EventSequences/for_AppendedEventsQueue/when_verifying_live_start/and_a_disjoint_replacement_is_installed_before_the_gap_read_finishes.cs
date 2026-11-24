// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_disjoint_replacement_is_installed_before_the_gap_read_finishes : given.a_queue_with_a_paused_gap_read
{
    static AppendedEvent OldFilterOnly() => MatchingAt(105);

    static AppendedEvent ReplacementAt(ulong sequence)
    {
        var @event = MatchingAt(sequence);
        return @event with { Context = @event.Context with { Tags = [new("replacement")] } };
    }

    void Establish() => _history = [ReplacementAt(103)];

    async Task Because()
    {
        await _queue.Enqueue([OldFilterOnly()]);
        await _readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _queue.Subscribe(_observerKey, [_eventType], new(["replacement"]));
        _resumeRead.TrySetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_deliver_the_live_event_past_the_missing_event() => _handledEvents.ShouldEqual(0);
    [Fact] void should_spill_the_replacement_subscription_to_catch_up() => _observer.Received(1).CatchUp();
}
