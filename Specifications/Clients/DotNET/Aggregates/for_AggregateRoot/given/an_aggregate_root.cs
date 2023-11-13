// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Aggregates.for_AggregateRoot.given;

public class an_aggregate_root : Specification
{
    protected AggregateRoot aggregate_root;
    protected Mock<IAggregateRootEventHandlers> event_handlers;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<ICausationManager> causation_manager;
    protected EventSourceId event_source_id;

    void Establish()
    {
        aggregate_root = new();
        event_handlers = new();
        event_sequence = new();
        causation_manager = new();

        event_source_id = Guid.NewGuid().ToString();
        aggregate_root._eventSourceId = event_source_id;

        aggregate_root.EventHandlers = event_handlers.Object;
        aggregate_root.EventSequence = event_sequence.Object;
        aggregate_root.CausationManager = causation_manager.Object;
    }
}
