// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_a_type_used_as_collection_and_object : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              Quantity = Quantity
          nested FeaturedLine
            from ItemAddedToOrder
              Quantity = Quantity
        """;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Lines": { "type": "array", "items": { "$ref": "#/$defs/OrderLine" } },
          "FeaturedLine": { "$ref": "#/$defs/OrderLine" }
        }, "$defs": { "OrderLine": { "title": "OrderLine", "type": "object", "properties": {
          "ProductId": { "type": "string", "format": "uuid" }, "Quantity": { "type": "integer" }
        } } } }
        """);

    void Because() => _result = GenerateModelBound(Declaration);

    [Fact] void should_emit_the_line_record_once() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Count(record => record.Identifier.Text == "OrderLine").ShouldEqual(1);
    [Fact] void should_explain_different_attribute_requirements() => _result.ShouldContain("Not expressible with model-bound attributes: conflicting projection scopes for record 'OrderLine'");
    [Fact] void should_compile() => CompileGenerated(GenerateDeclarative(Declaration), _result).ShouldNotBeNull();
}
