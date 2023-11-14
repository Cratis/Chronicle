// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProvider;

public class when_providing_an_aggregate_with_reducer_state_provider : given.an_aggregate_root_state_manager_and_two_events
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
        event_sequence
            .Setup(_ => _.GetForEventSourceIdAndEventTypes(aggregate_root._eventSourceId, event_types))
            .ReturnsAsync(events.ToImmutableList());
    }

    Task Because() => manager.Provide(aggregate_root);

    [Fact] void should_forward_to_reducer_handler() => reducer_handler.Verify(_ => _.OnNext(events, null));
    [Fact] void should_not_get_events() => event_sequence.Verify(_ => _.GetForEventSourceIdAndEventTypes(aggregate_root._eventSourceId, event_types), Once);
    [Fact] void should_set_state() => aggregate_root._state.ShouldEqual(state);
}
