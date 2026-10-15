// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_unexpressible_child_every_constant : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          children Lines identified by ProductId
            every
              Sku = "unknown"
            from ItemAddedToOrder key ProductId
        """;

    Contracts.Projections.ProjectionDefinition _declarative;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Lines": { "type": "array", "items": { "title": "OrderLine", "type": "object", "properties": {
            "ProductId": { "type": "string", "format": "uuid" }, "Sku": { "type": "string" }
          } } }
        } }
        """);

    void Because()
    {
        _result = GenerateDeclarative(Declaration);
        var assembly = CompileGenerated(_result, GenerateModelBound(Declaration));
        _declarative = CodeGeneration.given.generated_projection_definitions.Declarative(assembly);
    }

    [Fact] void should_explain_the_unexpressible_mapping_at_the_every_block() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'Sku'");
    [Fact] void should_not_call_unavailable_to_value() => _result.ShouldNotContain("ToValue");
    [Fact] void should_keep_child_subscription() => _declarative.Children["lines"].From.Single().Key.Id.ShouldEqual("ItemAddedToOrder");
    [Fact] void should_not_lower_the_constant_to_a_different_semantics() => _declarative.Children["lines"].All.Properties.ShouldBeEmpty();
}
