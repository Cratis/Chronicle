// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_nullable_properties : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested Details
            from OrderPlaced
              DisplayName = CustomerName
        """;

    Contracts.Projections.ProjectionDefinition _modelBound;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "ItemCount": { "type": ["integer", "null"] },
          "LastUpdated": { "type": ["string", "null"], "format": "date-time?" },
          "Identifier": { "type": "string", "format": "uuid?" },
          "Details": { "title": "OrderDetails", "type": ["object", "null"], "properties": {
            "DisplayName": { "type": ["string", "null"] }
          } },
          "Lines": { "type": ["array", "null"], "items": { "title": "OrderLine", "type": ["object", "null"], "properties": {
            "Quantity": { "type": ["integer", "null"] }
          } } }
        } }
        """);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_preserve_nullable_integer() => _result.ShouldContain("int? ItemCount");
    [Fact] void should_preserve_nullable_timestamp() => _result.ShouldContain("System.DateTimeOffset? LastUpdated");
    [Fact] void should_preserve_nullable_format_without_a_null_flag() => _result.ShouldContain("System.Guid? Identifier");
    [Fact] void should_preserve_nullable_object() => _result.ShouldContain("OrderDetails? Details");
    [Fact] void should_preserve_nullable_collection_and_items() => _result.ShouldContain("System.Collections.Generic.IEnumerable<OrderLine?>? Lines");
    [Fact] void should_emit_nullable_child_record() => _result.ShouldContain("public record OrderLine(");
    [Fact] void should_keep_nested_mapping() => _modelBound.Nested["details"].From.Single().Value.Properties["displayName"].ShouldEqual("customerName");
}
