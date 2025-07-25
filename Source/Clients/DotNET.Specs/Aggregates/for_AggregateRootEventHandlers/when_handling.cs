// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlers;

public class when_handling : given.aggregate_root_event_handlers
{
    IEnumerable<EventAndContext> _events;

    void Establish()
    {
        _events =
        [
            new EventAndContext(new FirstEventType("First"), EventContext.Empty),
            new EventAndContext(new SecondEventType("Second"), EventContext.Empty)
        ];
    }

    async Task Because() => await handlers.Handle(aggregate_root, _events);

    [Fact] void should_forward_to_handle_for_first_event_type() => aggregate_root.FirstEventTypeInstance.ShouldEqual(_events.First().Event);
    [Fact] void should_forward_to_handle_for_second_event_type() => aggregate_root.SecondEventTypeInstance.ShouldEqual(_events.Last().Event);
}
