// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_spilling_committed_events;

public class and_the_observer_is_already_catching_up : given.a_single_subscriber_with_an_event_type
{
    readonly TaskCompletionSource _caughtUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _catchUps;

    void Establish() => _observer.CatchUp().Returns(_ =>
    {
        Interlocked.Increment(ref _catchUps);
        _caughtUp.TrySetResult();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        // The first chunk hands it to catch-up; while it catches up it is not subscribed, so later chunks leave it alone.
        await _queue.SpillMatchingToCatchup([Event(7)]);
        await _caughtUp.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _queue.SpillMatchingToCatchup([Event(8)]);
        await _queue.SpillMatchingToCatchup([Event(9)]);
    }

    void Destroy() => _queue.Dispose();

    [Fact] void should_not_give_it_a_second_catch_up() => _catchUps.ShouldEqual(1);

    AppendedEvent Event(ulong sequenceNumber) =>
        AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "source", SequenceNumber = sequenceNumber } };
}
