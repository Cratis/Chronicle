// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.given;

public class a_builder : Specification
{
    protected IEventTypes _eventTypes;
    protected EventStoreSubscriptionBuilder _builder;
    protected EventStoreSubscriptionDefinition _result;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.All.Returns(ImmutableList.Create(new EventType("customer", 3)));
        _eventTypes.GetEventTypeFor(typeof(CustomerRegistered)).Returns(new EventType("customer", 3));
        _builder = new(_eventTypes, "subscription", "source");
    }

    protected record CustomerRegistered;
}
