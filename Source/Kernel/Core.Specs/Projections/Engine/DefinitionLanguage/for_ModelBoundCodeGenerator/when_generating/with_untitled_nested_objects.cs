// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_untitled_nested_objects : CodeGeneration.given.generated_schema_code
{
    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Items": { "type": "array", "items": { "type": "object", "properties": {
            "Detail": { "type": "object", "properties": { "Name": { "type": "string" } } }
          } } }
        } }
        """);

    void Because() => _result = GenerateModelBound(Subscription);

    [Fact] void should_use_the_resolved_parent_name() => _result.ShouldContain("OrderItemsDetail Detail");
    [Fact] void should_compile() => ShouldCompile(GenerateDeclarative(Subscription), _result);
}
