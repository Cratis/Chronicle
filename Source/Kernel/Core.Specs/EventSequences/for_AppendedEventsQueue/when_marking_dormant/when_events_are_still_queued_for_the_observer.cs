// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

public class when_events_are_still_queued_for_the_observer : given.a_queue_with_a_stored_durable_subscription
{
    readonly TaskCompletionSource _release = new();
    bool _result;

    async Task Establish()
    {
        await _queue.Subscribe(_observerKey, [_eventType]);
        _observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(_ => _release.Task);
        await _queue.Enqueue([EventAt(3)]);
        await Task.Delay(100);
    }

    async Task Because()
    {
        _result = await _queue.TryMarkDormant(_observerKey);
        _release.SetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_refuse_so_nothing_is_stranded() => _result.ShouldBeFalse();
}
