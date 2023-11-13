// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateManager;

public class when_handling_an_aggregate_with_reducer_state_provider : given.an_aggregate_root_state_manager_and_two_events
{
    Mock<IReducerHandler> reducer_handler;
    StateForAggregateRoot state;

    void Establish()
    {
        reducers_registrar.Setup(_ => _.HasReducerFor(typeof(StateForAggregateRoot))).Returns(true);
        immediate_projections.Setup(_ => _.HasProjectionFor(typeof(StateForAggregateRoot))).Returns(false);

        reducer_handler = new();
        reducers_registrar.Setup(_ => _.GetForModelType(typeof(StateForAggregateRoot))).Returns(reducer_handler.Object);
        state = new StateForAggregateRoot("Something");
        reducer_handler.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), null)).Returns(Task.FromResult(new InternalReduceResult(state, EventSequenceNumber.Unavailable)));
    }

    Task Because() => manager.Handle(aggregate_root, events);

    [Fact] void should_forward_to_reducer_handler() => reducer_handler.Verify(_ => _.OnNext(events, null));
    [Fact] void should_set_state() => aggregate_root._state.ShouldEqual(state);
}
