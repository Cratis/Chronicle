// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_event_source_id_mappings : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
            Source = $eventSourceId
            ContextSource = $eventContext.eventSourceId
            Created = $eventContext.occurred
        """;

    Contracts.Projections.ProjectionDefinition _modelBound;
    Contracts.Projections.ProjectionDefinition _declarative;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string", "format": "uuid" },
          "Source": { "type": "string", "format": "uuid" },
          "ContextSource": { "type": "string", "format": "uuid" },
          "Created": { "type": "string", "format": "date-time" }
        } }
        """);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
        _declarative = CodeGeneration.given.generated_projection_definitions.Declarative(assembly);
    }

    [Fact] void should_emit_the_event_source_id_attribute_without_arguments() => _result.ShouldContain("[SetFromEventSourceId<OrderPlaced>]");
    [Fact] void should_keep_the_context_event_source_id_attribute() => _result.ShouldContain("[SetFromContext<OrderPlaced>(\"EventSourceId\")]");
    [Fact] void should_keep_the_other_context_property_attribute() => _result.ShouldContain("[SetFromContext<OrderPlaced>(\"Occurred\")]");
    [Fact] void should_round_trip_the_event_source_id_expression() => _modelBound.From.Single().Value.Properties["source"].ShouldEqual("$eventSourceId");
    [Fact] void should_round_trip_the_context_event_source_id_expression() => _modelBound.From.Single().Value.Properties["contextSource"].ShouldEqual("$eventContext(eventSourceId)");
    [Fact] void should_round_trip_the_other_context_expression() => _modelBound.From.Single().Value.Properties["created"].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_match_the_declarative_mappings() => _modelBound.From.Single().Value.Properties.ShouldContainOnly(_declarative.From.Single().Value.Properties.ToArray());
}
