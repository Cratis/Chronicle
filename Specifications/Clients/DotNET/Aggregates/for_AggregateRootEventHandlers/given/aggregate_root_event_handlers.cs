// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.given;

public class aggregate_root_event_handlers : Specification
{
    protected AggregateRootEventHandlers handlers;
    protected StatelessAggregateRoot aggregate_root;
    protected Mock<IEventTypes> event_types;

    void Establish()
    {
        aggregate_root = new();
        event_types = new();

        handlers = new AggregateRootEventHandlers(event_types.Object, typeof(StatelessAggregateRoot));
    }
}
