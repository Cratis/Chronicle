// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_event_source_type_filter;

public class and_default_excludes_another_source_type : given.a_single_subscriber_with_event_source_type_filter
{
    AppendedEvent _appendedEvent;

    protected override ObserverFilters? Filters => new([], EventSourceType.Default);

    void Establish()
    {
        _appendedEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = _eventType, EventSourceType = "order", EventSourceId = Guid.NewGuid() }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_dispatch_a_non_default_event() => _handledEventsPerPartition.ShouldBeEmpty();
}
