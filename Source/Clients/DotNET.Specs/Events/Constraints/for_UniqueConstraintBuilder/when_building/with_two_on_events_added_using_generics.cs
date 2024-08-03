// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_two_on_events_added_using_generics : given.a_unique_constraint_builder_with_owner
{
    UniqueConstraintDefinition _result;
    EventType _firstEventType;
    EventType _secondEventType;

    void Establish()
    {
        _firstEventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(_firstEventType);
        _secondEventType = new EventType(nameof(AnotherEventWithStringProperty), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(AnotherEventWithStringProperty)).Returns(_secondEventType);

        _constraintBuilder.On<EventWithStringProperty>(_ => _.SomeProperty);
        _constraintBuilder.On<AnotherEventWithStringProperty>(_ => _.SomeProperty);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_have_two_event_types_and_properties() => _result.EventsWithProperties.Count().ShouldEqual(2);
    [Fact] void should_have_first_event_type() => _result.EventsWithProperties.First().EventType.ShouldEqual(_firstEventType);
    [Fact] void should_have_first_event_property() => _result.EventsWithProperties.First().Property.ShouldEqual(nameof(EventWithStringProperty.SomeProperty));
    [Fact] void should_have_second_event_type() => _result.EventsWithProperties.Last().EventType.ShouldEqual(_secondEventType);
    [Fact] void should_have_second_event_property() => _result.EventsWithProperties.Last().Property.ShouldEqual(nameof(AnotherEventWithStringProperty.SomeProperty));
}
