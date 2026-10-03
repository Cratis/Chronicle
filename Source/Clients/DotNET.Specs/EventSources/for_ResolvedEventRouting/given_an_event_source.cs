// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_ResolvedEventRouting;

public class given_an_event_source : Specification
{
    protected IEventSources _eventSources;
    protected EventSourceDefinition _definition;

    void Establish()
    {
        _definition = new(
            typeof(for_EventSources.ShoppingCartEventSource),
            "ShoppingCart",
            string.Empty,
            ConcurrencyDimensions.EventSourceId,
            [new EventStream("Items", string.Empty, ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]);
        _eventSources = Substitute.For<IEventSources>();
        _eventSources.GetFor(typeof(for_EventSources.ShoppingCartEventSource)).Returns(_definition);
        _eventSources.GetFor(typeof(string)).Returns(_ => throw new UnknownEventSource("System.String"));
    }
}
