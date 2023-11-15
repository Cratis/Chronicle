// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.when_applying_to_stateless_aggregate_root;

public class a_valid_event : given.a_stateless_aggregate_root
{
    FirstEventType event_to_apply;
    object event_applied;

    void Establish()
    {
        event_handlers.Setup(_ => _.Handle(aggregate_root, IsAny<IEnumerable<EventAndContext>>())).Returns((IAggregateRoot _, IEnumerable<EventAndContext> events) =>
        {
            event_applied = events.First().Event;
            return Task.CompletedTask;
        });
        event_to_apply = new(Guid.NewGuid().ToString());
    }

    void Because() => aggregate_root.Apply(event_to_apply);

    [Fact] void should_forward_to_event_handlers() => event_applied.ShouldEqual(event_to_apply);
}
