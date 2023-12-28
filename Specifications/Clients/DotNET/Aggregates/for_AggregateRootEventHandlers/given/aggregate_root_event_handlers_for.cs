// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications;

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.given;

public class aggregate_root_event_handlers_for<TAggregateRoot> : Specification
    where TAggregateRoot : AggregateRoot
{
    protected AggregateRootEventHandlers handlers;
    protected IEventTypes event_types;

    void Establish()
    {
        event_types = new EventTypesForSpecifications(GetEventTypes());
        handlers = new AggregateRootEventHandlers(event_types, typeof(TAggregateRoot));
    }

    protected virtual IEnumerable<Type> GetEventTypes() => Enumerable.Empty<Type>();
}
