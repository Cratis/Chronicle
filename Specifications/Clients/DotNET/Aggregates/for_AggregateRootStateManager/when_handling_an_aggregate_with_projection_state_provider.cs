// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateManager;

public class when_handling_an_aggregate_with_projection_state_provider : given.an_aggregate_root_state_manager_and_two_events
{
    StateForAggregateRoot state;

    void Establish()
    {
        reducers_registrar.Setup(_ => _.HasReducerFor(typeof(StateForAggregateRoot))).Returns(false);
        immediate_projections.Setup(_ => _.HasProjectionFor(typeof(StateForAggregateRoot))).Returns(true);
        state = new StateForAggregateRoot("Something");
        immediate_projections.Setup(_ => _.GetInstanceById(aggregate_root.StateType, aggregate_root._eventSourceId)).Returns(Task.FromResult(new ImmediateProjectionResult(state, Enumerable.Empty<PropertyPath>(), 0)));
    }

    Task Because() => manager.Handle(aggregate_root, events);

    [Fact] void should_set_state() => aggregate_root._state.ShouldEqual(state);
}
