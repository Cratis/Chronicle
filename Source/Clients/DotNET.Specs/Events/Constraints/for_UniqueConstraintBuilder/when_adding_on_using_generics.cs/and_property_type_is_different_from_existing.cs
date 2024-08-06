// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_generics;

public class and_property_type_is_different_from_existing : given.a_unique_constraint_builder_with_owner
{
    EventType _firstEventType;
    EventType _secondEventType;

    PropertyTypeMismatchInUniqueConstraint _result;

    void Establish()
    {
        _firstEventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(_firstEventType);

        _eventTypes.GetSchemaFor(_firstEventType.Id).Returns(_generator.Generate(typeof(EventWithStringProperty)));

        _secondEventType = new EventType(nameof(EventWithIntProperty), EventGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithIntProperty)).Returns(_secondEventType);

        _eventTypes.GetSchemaFor(_secondEventType.Id).Returns(_generator.Generate(typeof(EventWithIntProperty)));

        _constraintBuilder.On<EventWithStringProperty>(_ => _.SomeProperty);
    }

    void Because() => _result = Catch.Exception<PropertyTypeMismatchInUniqueConstraint>(() => _constraintBuilder.On<EventWithIntProperty>(_ => _.SomeProperty));

    [Fact] void should_throw_property_type_mismatch_in_unique_constraint() => _result.ShouldNotBeNull();
    [Fact] void should_have_second_event_type_in_exception() => _result.EventType.ShouldEqual(_secondEventType);
    [Fact] void should_have_property_in_exception() => _result.Property.ShouldEqual(nameof(EventWithIntProperty.SomeProperty).ToCamelCase());
}
