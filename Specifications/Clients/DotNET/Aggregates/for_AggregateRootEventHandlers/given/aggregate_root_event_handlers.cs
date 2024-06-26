// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlers.given;

public class aggregate_root_event_handlers : Specification
{
    protected AggregateRootEventHandlers handlers;
    protected StatelessAggregateRoot aggregate_root;
    protected IEventTypes event_types;

    void Establish()
    {
        aggregate_root = new();
        event_types = new EventTypesForSpecifications();

        handlers = new AggregateRootEventHandlers(event_types, typeof(StatelessAggregateRoot));
    }
}
