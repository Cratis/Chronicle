// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.when_applying_to_stateful_aggregate_root;

public class a_valid_event : given.a_stateful_aggregate_root
{
    FirstEventType event_to_apply;
    StateForAggregateRoot state;

    void Establish()
    {
        event_to_apply = new(Guid.NewGuid().ToString());
        aggregate_root._state = new StateForAggregateRoot(Guid.NewGuid().ToString());
        state = new StateForAggregateRoot(Guid.NewGuid().ToString());

        state_provider.Setup(_ => _.Update(aggregate_root._state, new[] { event_to_apply })).Returns(Task.FromResult<object?>(state));
    }

    void Because() => aggregate_root.Apply(event_to_apply);

    [Fact] void should_not_forward_to_event_handlers() => event_handlers.Verify(_ => _.Handle(aggregate_root, IsAny<IEnumerable<EventAndContext>>()), Never);
    [Fact] void should_update_the_state() => aggregate_root._state.ShouldEqual(state);
}
