// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder;

public class when_adding_event_type_by_id : Specification
{
    IEventTypes _eventTypes;
    EventStoreSubscriptionId _subscriptionId;
    EventStoreSubscriptionBuilder _builder;
    EventTypeId _eventTypeId;
    EventStoreSubscriptionDefinition _result;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _subscriptionId = "my-subscription";
        _eventTypeId = "some-event-type-id";
        _builder = new EventStoreSubscriptionBuilder(_eventTypes, _subscriptionId, "source-event-store");
    }

    void Because()
    {
        _builder.WithEventType(_eventTypeId);
        _result = _builder.Build();
    }

    [Fact] void should_include_the_event_type_id_in_the_definition() => _result.EventTypes.ShouldContain(_eventTypeId);
    [Fact] void should_have_exactly_one_event_type() => _result.EventTypes.Count().ShouldEqual(1);
}
