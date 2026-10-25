// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable.given;

public class an_unreachable_observer : for_AppendedEventsQueue.given.a_single_subscriber_with_queue_telemetry
{
    protected readonly CatchupClock _clock = new();
    protected AppendedEvent _firstEvent;
    protected AppendedEvent _laterEvent;

    void Establish()
    {
        _queue.CatchupTimeProvider = _clock;
        _firstEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "first", SequenceNumber = 0UL }
        };
        _laterEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "later", SequenceNumber = 1UL }
        };
        _observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(Task.FromException(new InvalidOperationException("observer unreachable")));
    }

    protected async Task DispatchFirstEvent()
    {
        await _queue.Enqueue([_firstEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    void Destroy() => _queue.Dispose();
}
