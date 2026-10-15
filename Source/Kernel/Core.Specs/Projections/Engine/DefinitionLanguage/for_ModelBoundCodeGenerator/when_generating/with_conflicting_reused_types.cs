// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_conflicting_reused_types : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested BillingAddress
            from OrderPlaced
              Name = CustomerName
          nested ShippingAddress
            from ProductRegistered
              Name = Name
        """;

    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "BillingAddress": { "$ref": "#/$defs/Address" },
          "ShippingAddress": { "$ref": "#/$defs/Address" }
        }, "$defs": { "Address": { "title": "Address", "type": "object", "properties": {
          "Name": { "type": "string" }
        } } } }
        """);

    void Because() => _result = GenerateModelBound(Declaration);

    [Fact] void should_explain_conflicting_scopes() => _result.ShouldContain("Not expressible with model-bound attributes: conflicting projection scopes for record 'Address'");
    [Fact] void should_not_silently_keep_the_first_mapping() => _result.ShouldNotContain("SetFrom<OrderPlaced>");
    [Fact] void should_compile() => CompileGenerated(GenerateDeclarative(Declaration), _result).ShouldNotBeNull();
}
