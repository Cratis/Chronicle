// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.given;

public class a_stateless_aggregate_root : all_dependencies
{
    protected AggregateRoot aggregate_root;
    protected EventSourceId event_source_id;
    protected Mock<IAggregateRootStateProvider> state_provider;

    void Establish()
    {
        aggregate_root = new();

        event_source_id = Guid.NewGuid().ToString();
        state_provider = new();

        aggregate_root._eventSourceId = event_source_id;
        aggregate_root.EventHandlers = event_handlers.Object;
        aggregate_root.EventSequence = event_sequence.Object;
        aggregate_root.CausationManager = causation_manager.Object;
        aggregate_root.StateProvider = state_provider.Object;
    }
}
