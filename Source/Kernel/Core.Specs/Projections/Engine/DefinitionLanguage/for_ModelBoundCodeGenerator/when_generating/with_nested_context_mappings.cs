// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_nested_context_mappings : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested Details
            every
              LastUpdated = $eventContext.occurred
            from OrderPlaced
              Created = $eventContext.occurred
        """;

    Contracts.Projections.ProjectionDefinition _modelBound;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Details": { "title": "OrderDetails", "type": "object", "properties": {
            "Created": { "type": "string", "format": "date-time" },
            "LastUpdated": { "type": "string", "format": "date-time" }
          } }
        } }
        """);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_target_the_nested_property() => _result.ShouldContain("[property: SetFromContext<OrderPlaced>(\"Occurred\")]");
    [Fact] void should_keep_nested_every_context_mapping() => _modelBound.Nested["details"].From.Single().Value.Properties["lastUpdated"].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_keep_nested_from_context_mapping() => _modelBound.Nested["details"].From.Single().Value.Properties["created"].ShouldEqual("$eventContext(occurred)");
}
