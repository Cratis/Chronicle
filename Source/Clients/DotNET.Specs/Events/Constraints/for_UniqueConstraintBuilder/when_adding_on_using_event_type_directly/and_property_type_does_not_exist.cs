// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

public class and_property_type_does_not_exist : given.a_unique_constraint_builder_with_owner
{
    EventType _eventType;

    PropertyDoesNotExistOnEventType _result;

    void Establish()
    {
        var generator = new NJsonSchemaGenerator(new JsonSchemaGeneratorSettings());
        _eventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        var firstEventTypeSchema = generator.Generate(typeof(EventWithStringProperty));
        _eventTypes.GetSchemaFor(_eventType.Id).Returns(firstEventTypeSchema);
    }

    void Because() => _result = Catch.Exception<PropertyDoesNotExistOnEventType>(() => _constraintBuilder.On(_eventType, "NonExistingProperty"));

    [Fact] void should_throw_property_type_mismatch_in_unique_constraint() => _result.ShouldNotBeNull();
    [Fact] void should_have_event_type_in_exception() => _result.EventType.ShouldEqual(_eventType);
    [Fact] void should_have_property_in_exception() => _result.Property.ShouldEqual("NonExistingProperty");
}
