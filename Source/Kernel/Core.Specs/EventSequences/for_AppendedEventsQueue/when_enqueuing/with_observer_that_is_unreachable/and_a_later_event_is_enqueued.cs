// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_a_later_event_is_enqueued : given.an_unreachable_observer
{
    async Task Because()
    {
        await DispatchFirstEvent();
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    [Fact] void should_not_deliver_the_later_event_to_the_spilled_observer() =>
        _observer.DidNotReceive().Handle((Key)_laterEvent.Context.EventSourceId, Arg.Any<IEnumerable<AppendedEvent>>());
}
