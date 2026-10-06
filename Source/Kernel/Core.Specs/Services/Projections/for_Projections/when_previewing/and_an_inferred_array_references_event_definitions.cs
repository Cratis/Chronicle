// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_an_inferred_array_references_event_definitions : given.an_inferred_array_preview
{
    void Establish() => _arrayEventSchema = JsonSchema.FromJson("""
        { "type": "object", "properties": { "items": { "$ref": "#/$defs/Items" } },
          "$defs": {
            "Items": { "type": "array", "items": { "$ref": "#/$defs/Item" } },
            "Item": { "type": "object", "properties": {
              "name": { "type": "string" }, "quantity": { "$ref": "#/$defs/Quantity" },
              "parent": { "$ref": "#/$defs/Item" }
            } },
            "Quantity": { "type": "integer", "format": "int32", "minimum": 1 }
          } }
        """);

    async Task Because() => _error = await Catch.Exception(Preview);

    [Fact] void should_preview_without_a_conversion_failure() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_referenced_child_value() => PreviewedChild()["name"]!.GetValue<string>().ShouldEqual("Book");
    [Fact] void should_convert_using_the_referenced_format() => PreviewedChild()["quantity"]!.GetValue<int>().ShouldEqual(3);
    [Fact] void should_resolve_the_referenced_array_schema() => InferredSchema().Properties["items"].ActualTypeSchema.IsArray.ShouldBeTrue();
    [Fact] void should_preserve_recursive_references_without_expanding_forever() => InferredSchema().Properties["items"].Item!.Properties["parent"].Reference.ShouldNotBeNull();
}
