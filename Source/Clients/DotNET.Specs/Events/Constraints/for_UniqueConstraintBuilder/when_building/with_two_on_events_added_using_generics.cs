// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;
using Cratis.Strings;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_two_on_events_added_using_generics : given.a_unique_constraint_builder_with_owner
{
    UniqueConstraintDefinition _result;
    EventType _firstEventType;
    EventType _secondEventType;

    void Establish()
    {
        _firstEventType = new EventType(nameof(EventWithStringProperty), EventTypeGeneration.First);
        _eventTypes.GetSchemaFor(_firstEventType.Id).Returns(_generator.Generate(typeof(EventWithStringProperty)));
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(_firstEventType);
        _secondEventType = new EventType(nameof(AnotherEventWithStringProperty), EventTypeGeneration.First);
        _eventTypes.GetSchemaFor(_secondEventType.Id).Returns(_generator.Generate(typeof(AnotherEventWithStringProperty)));
        _eventTypes.GetEventTypeFor(typeof(AnotherEventWithStringProperty)).Returns(_secondEventType);

        _constraintBuilder.On<EventWithStringProperty>(_ => _.SomeProperty, _ => _.SomeOtherProperty);
        _constraintBuilder.On<AnotherEventWithStringProperty>(_ => _.SomeProperty, _ => _.SomeOtherProperty);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_have_two_event_types_and_properties() => _result.EventsWithProperties.Count().ShouldEqual(2);
    [Fact] void should_have_first_event_type() => _result.EventsWithProperties.First().EventTypeId.ShouldEqual(_firstEventType.Id);
    [Fact] void should_have_first_event_first_property() => _result.EventsWithProperties.First().Properties.ToArray()[0].ShouldEqual(nameof(EventWithStringProperty.SomeProperty));
    [Fact] void should_have_first_event_second_property() => _result.EventsWithProperties.First().Properties.ToArray()[1].ShouldEqual(nameof(EventWithStringProperty.SomeOtherProperty));
    [Fact] void should_have_second_event_type() => _result.EventsWithProperties.Last().EventTypeId.ShouldEqual(_secondEventType.Id);
    [Fact] void should_have_second_event_first_property() => _result.EventsWithProperties.Last().Properties.ToArray()[0].ShouldEqual(nameof(AnotherEventWithStringProperty.SomeProperty));
    [Fact] void should_have_second_event_second_property() => _result.EventsWithProperties.Last().Properties.ToArray()[1].ShouldEqual(nameof(AnotherEventWithStringProperty.SomeOtherProperty));
}
