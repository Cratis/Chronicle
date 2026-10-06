// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_an_inferred_property_is_a_nonempty_object_array : given.an_inferred_array_preview
{
    async Task Because() => _error = await Catch.Exception(Preview);

    [Fact] void should_preview_without_a_conversion_failure() => _error.ShouldBeNull();
    [Fact] void should_keep_the_child_properties() => PreviewedChild()["name"]!.GetValue<string>().ShouldEqual("Book");
    [Fact] void should_convert_using_the_child_format() => PreviewedChild()["quantity"]!.GetValue<int>().ShouldEqual(3);
    [Fact] void should_preserve_the_item_schema() => InferredSchema().Properties["items"].Item!.Properties["quantity"].Format.ShouldEqual("int32");
    [Fact] void should_preserve_the_complete_child_schema() => System.Text.Json.Nodes.JsonNode.Parse(InferredSchema().Properties["items"].Item!.ToJson())!["required"]![0]!.GetValue<string>().ShouldEqual("name");
    [Fact] void should_not_treat_default_values_as_schema_references() => System.Text.Json.Nodes.JsonNode.Parse(InferredSchema().Properties["items"].Item!.ToJson())!["default"]!["$ref"]!.GetValue<string>().ShouldEqual("literal");
}
