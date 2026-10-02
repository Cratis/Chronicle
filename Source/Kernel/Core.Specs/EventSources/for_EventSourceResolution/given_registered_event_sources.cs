// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSources;
using Cratis.Chronicle.Storage.EventSources;

namespace Cratis.Chronicle.EventSources.for_EventSourceResolution;

public class given_registered_event_sources : Specification
{
    protected IEventSourcesStorage _eventSources;
    protected EventSourceDefinition _cart;

    void Establish()
    {
        _cart = new(
            "ShoppingCart",
            "A cart",
            EventSourceOwner.Client,
            ConcurrencyDimensions.EventSourceId,
            [new EventStreamDefinition("Items", "Items", ConcurrencyDimensions.None)]);
        _eventSources = Substitute.For<IEventSourcesStorage>();
        _eventSources.Find(new EventSourceName("ShoppingCart")).Returns(_cart);
    }
}
