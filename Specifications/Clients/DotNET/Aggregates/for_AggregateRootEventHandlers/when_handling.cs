// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers;

public class when_handling : given.aggregate_root_event_handlers
{
    IEnumerable<EventAndContext> events;

    void Establish()
    {
        events = new[]
        {
            new EventAndContext(new FirstEventType("First"), EventContext.Empty),
            new EventAndContext(new SecondEventType("Second"), EventContext.Empty)
        };
    }

    async Task Because() => await handlers.Handle(aggregate_root, events);

    [Fact] void should_forward_to_handle_for_first_event_type() => aggregate_root.FirstEventTypeInstance.ShouldEqual(events.First().Event);
    [Fact] void should_forward_to_handle_for_second_event_type() => aggregate_root.SecondEventTypeInstance.ShouldEqual(events.Last().Event);
}
