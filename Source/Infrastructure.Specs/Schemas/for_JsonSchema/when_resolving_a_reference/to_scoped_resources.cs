// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_scoped_resources
{
    const string Document = """
        {
          "type":"object",
          "properties":{"value":{}},
          "Person":{"type":"string","title":"legacy"},
          "$defs":{
            "personal":{"$id":"personal","type":"object","title":"resource","properties":{"x":{"type":"string","title":"member"}},"$defs":{"person":{"$anchor":"Person","type":"string","title":"foreign"}}},
            "root":{"$anchor":"RootPerson","type":"string","title":"root-anchor"},
            "nested":{"$id":"nested/","$defs":{"child":{"$id":"child","type":"string","title":"nested-resource"}}}
          }
        }
        """;

    [Theory]
    [InlineData("personal", "resource")]
    [InlineData("personal#/properties/x", "member")]
    [InlineData("personal#Person", "foreign")]
    [InlineData("#RootPerson", "root-anchor")]
    [InlineData("#Person", "legacy")]
    [InlineData("#$defs/root", "root-anchor")]
    [InlineData("#definitions/Person", "definition")]
    [InlineData("#properties/Person", "property")]
    [InlineData("nested/child", "nested-resource")]
    public void should_resolve_only_within_the_requested_resource(string reference, string title)
    {
        var schema = Create(reference);
        schema.Properties["value"].Reference?.Title.ShouldEqual(title);
    }

    [Theory]
    [InlineData("#PersonOnlyInForeign")]
    [InlineData("missing")]
    [InlineData("https://external.invalid/schema")]
    [InlineData("https://external.invalid/schema#Person")]
    [InlineData("personal#RootPerson")]
    public void should_not_fetch_or_cross_resource_boundaries(string reference)
    {
        var schema = Create(reference);
        schema.Node.Remove("Person");
        schema.Node["$defs"]!["personal"]!["$defs"]!["person"]!["$anchor"] = "PersonOnlyInForeign";
        schema.Properties["value"].Reference.ShouldBeNull();
    }

    [Fact]
    public void should_resolve_an_indexed_absolute_resource_without_fetching()
    {
        var schema = Create("https://example.invalid/base/personal");
        schema.Node["$id"] = "https://example.invalid/base/root";
        schema.Properties["value"].Reference?.Title.ShouldEqual("resource");
    }

    [Fact]
    public void should_not_index_ids_or_anchors_in_instance_annotations()
    {
        var schema = Create("annotation");
        schema.Node["default"] = JsonNode.Parse("""{"$id":"annotation","$anchor":"Annotation","type":"string"}""");
        schema.Properties["value"].Reference.ShouldBeNull();
    }

    [Fact]
    public void should_terminate_for_a_cyclic_resource()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"$ref":"personal"}},"$defs":{"personal":{"$id":"personal","$ref":"personal"}}}""");
        var resolved = schema.Properties["value"].Reference;
        for (var index = 0; index < 64; index++) resolved = resolved?.Reference;
        resolved.ShouldNotBeNull();
        schema.HasSchemaMetadata().ShouldBeFalse();
    }

    [Fact]
    public void should_bound_the_resource_index_depth()
    {
        var schema = Create("too-deep");
        var node = schema.Node;
        for (var index = 0; index < 66; index++)
        {
            var child = new JsonObject();
            node["not"] = child;
            node = child;
        }
        node["$id"] = "too-deep";
        node["type"] = "string";
        schema.Properties["value"].Reference.ShouldBeNull();
    }

    static JsonSchema Create(string reference)
    {
        var schema = JsonSchema.FromJson(Document);
        schema.Node["properties"]!["value"]!["$ref"] = reference;
        schema.Node["definitions"] = JsonNode.Parse("""{"Person":{"type":"string","title":"definition"}}""");
        schema.Node["properties"]!["Person"] = JsonNode.Parse("""{"type":"string","title":"property"}""");
        return schema;
    }
}
