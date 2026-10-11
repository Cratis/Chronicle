// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_reused_nested_types : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested BillingAddress
            from OrderPlaced
              Name = CustomerName
          nested ShippingAddress
            from OrderPlaced
              Name = CustomerName
        """;

    Contracts.Projections.ProjectionDefinition _modelBound;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "BillingAddress": { "$ref": "#/$defs/Address" },
          "ShippingAddress": { "$ref": "#/$defs/Address" }
        }, "$defs": { "Address": { "title": "Address", "type": "object", "properties": {
          "Name": { "type": "string" }
        } } } }
        """);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_emit_the_address_record_once() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Count(record => record.Identifier.Text == "Address").ShouldEqual(1);
    [Fact] void should_keep_billing_mapping() => _modelBound.Nested["billingAddress"].From.Single().Value.Properties["name"].ShouldEqual("customerName");
    [Fact] void should_keep_shipping_mapping() => _modelBound.Nested["shippingAddress"].From.Single().Value.Properties["name"].ShouldEqual("customerName");
}
