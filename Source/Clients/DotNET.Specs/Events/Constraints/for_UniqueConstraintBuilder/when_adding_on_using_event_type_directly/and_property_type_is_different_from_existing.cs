// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

public class and_property_type_is_different_from_existing : given.a_unique_constraint_builder_with_owner
{
    EventType _firstEventType;
    EventType _secondEventType;

    PropertyTypeMismatchInUniqueConstraint _result;

    void Establish()
    {
        var generator = new NJsonSchemaGenerator(new JsonSchemaGeneratorSettings());
        _firstEventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        var firstEventTypeSchema = generator.Generate(typeof(EventWithStringProperty));
        _eventTypes.GetSchemaFor(_firstEventType.Id).Returns(firstEventTypeSchema);
        _secondEventType = new EventType(nameof(EventWithIntProperty), EventGeneration.First);
        var secondEventTypeSchema = generator.Generate(typeof(EventWithIntProperty));
        _eventTypes.GetSchemaFor(_secondEventType.Id).Returns(secondEventTypeSchema);

        _constraintBuilder.On(_firstEventType, nameof(EventWithStringProperty.SomeProperty));
    }

    void Because() => _result = Catch.Exception<PropertyTypeMismatchInUniqueConstraint>(() => _constraintBuilder.On(_secondEventType, nameof(EventWithIntProperty.SomeProperty)));

    [Fact] void should_throw_property_type_mismatch_in_unique_constraint() => _result.ShouldNotBeNull();
    [Fact] void should_have_second_event_type_in_exception() => _result.EventType.ShouldEqual(_secondEventType);
    [Fact] void should_have_property_in_exception() => _result.Property.ShouldEqual(nameof(EventWithIntProperty.SomeProperty));
}
