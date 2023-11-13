// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProvider.given;

public class an_aggregate_root_state_manager : Specification
{
    protected AggregateRootStateProvider manager;
    protected Mock<IReducersRegistrar> reducers_registrar;
    protected Mock<IImmediateProjections> immediate_projections;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IAggregateRootEventHandlers> event_handlers;

    protected StatefulAggregateRoot aggregate_root;

    void Establish()
    {
        reducers_registrar = new();
        immediate_projections = new();

        manager = new AggregateRootStateProvider(reducers_registrar.Object, immediate_projections.Object);

        event_handlers = new();
        event_sequence = new();
        aggregate_root = new()
        {
            _eventSourceId = Guid.NewGuid().ToString(),
            EventSequence = event_sequence.Object,
            EventHandlers = event_handlers.Object
        };
    }
}
