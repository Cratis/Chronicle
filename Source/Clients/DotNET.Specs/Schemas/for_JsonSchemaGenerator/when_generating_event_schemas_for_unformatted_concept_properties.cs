// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_event_schemas_for_unformatted_concept_properties : given.a_json_schema_generator
{
    JsonSchema[] _results;

    void Establish() => _generator.GenerateForReadModel(typeof(given.TypeWithUnformattedConcepts));

    void Because()
    {
        var type = typeof(given.TypeWithUnformattedConcepts);
        var compensationType = typeof(given.CompensatingUnformattedConcepts);
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(type).Returns(new EventType("registered-event", EventTypeGeneration.First));
        _results =
        [
            _generator.Generate(type),
            _generator.GenerateLegacyEventType(type),
            _generator.Generate(type, eventTypes),
            _generator.GenerateLegacyEventType(type, eventTypes),
            _generator.Generate(compensationType, eventTypes).ActualProperties["Concepts"],
            _generator.GenerateLegacyEventType(compensationType, eventTypes).ActualProperties["Concepts"]
        ];
    }

    [Fact] void should_keep_nullable_string_concepts_string_only() => TypesFor("NullableString").ShouldEqual(Enumerable.Repeat(JsonObjectType.String, 6));
    [Fact] void should_keep_nullable_bool_concepts_boolean_only() => TypesFor("NullableBool").ShouldEqual(Enumerable.Repeat(JsonObjectType.Boolean, 6));
    [Fact] void should_keep_non_nullable_string_concepts_string_only() => TypesFor("NonNullableString").ShouldEqual(Enumerable.Repeat(JsonObjectType.String, 6));
    [Fact] void should_keep_non_nullable_bool_concepts_boolean_only() => TypesFor("NonNullableBool").ShouldEqual(Enumerable.Repeat(JsonObjectType.Boolean, 6));

    IEnumerable<JsonObjectType> TypesFor(string property) => _results.Select(schema => schema.ActualProperties[property].Type);
}
