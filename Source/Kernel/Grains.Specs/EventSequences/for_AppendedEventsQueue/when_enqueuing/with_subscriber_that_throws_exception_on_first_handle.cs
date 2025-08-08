// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_subscriber_that_throws_exception_on_first_handle : given.a_single_subscriber_with_an_event_type
{
    AppendedEvent _appendedEvent;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _appendedEvent = AppendedEvent.Empty();
        _eventSourceId = Guid.NewGuid();
        _appendedEvent = _appendedEvent with
        {
            Context = EventContext.Empty with
            {
                EventType = _eventType,
                EventSourceId = _eventSourceId
            }
        };

        var callCount = 0;
        _observer
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(_ =>
            {
                switch (callCount++)
                {
                    case 0:
                        throw new Exception();
                    case 1:
                        break;
                }
            });
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_twice() => _handledEventsPerPartition[_eventSourceId].Count.ShouldEqual(2);
}
