// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_TagExtensions.given;

public class an_event_source_filter : Specification
{
    protected IEventSources _eventSources;

    void Establish()
    {
        var definition = new EventSourceDefinition(
            typeof(for_EventSources.ShoppingCartEventSource),
            "ShoppingCart",
            string.Empty,
            ConcurrencyDimensions.EventSourceId,
            [new EventStream("Items", string.Empty, ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]);
        _eventSources = Substitute.For<IEventSources>();
        _eventSources.GetFor(typeof(for_EventSources.ShoppingCartEventSource)).Returns(definition);
    }

    [FromEventSource<for_EventSources.ShoppingCartEventSource>("Items")]
    protected class ItemsReactor;

    [FromEventSource<for_EventSources.ShoppingCartEventSource>("Payment")]
    protected class PaymentReactor;
}
