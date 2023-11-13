// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.when_applying;

public class a_valid_event : given.an_aggregate_root
{
    [EventType("d1faeb5d-2951-484a-89d5-bff35a357514")]
    record MyEvent(string Something);

    MyEvent event_to_apply;
    object event_applied;

    void Establish()
    {
        aggregate_root.EventHandlers = event_handlers.Object;
        event_handlers.Setup(_ => _.Handle(aggregate_root, IsAny<IEnumerable<EventAndContext>>())).Returns((IAggregateRoot _, IEnumerable<EventAndContext> events) =>
        {
            event_applied = events.First().Event;
            return Task.CompletedTask;
        });
        event_to_apply = new(Guid.NewGuid().ToString());
    }

    async Task Because() => await aggregate_root.Apply(event_to_apply);

    [Fact] void should_forward_to_event_handlers() => event_applied.ShouldEqual(event_to_apply);
}
