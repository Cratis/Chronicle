// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_getting_schema_for_property_path;

// A self-referential type (Feature -> SubFeatures of Feature) is emitted by System.Text.Json with the nested
// collection's item as an in-document JSON Pointer to the first occurrence. Resolving the schema for the nested
// path must follow that pointer so the nested item schema (with its properties) is returned — otherwise the
// child's identity/properties cannot be converted correctly when persisted.
public class with_self_referential_nested_children : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": {
            "features": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "id": { "type": "string", "format": "guid" },
                        "name": { "type": "string" },
                        "subFeatures": {
                            "type": "array",
                            "items": { "$ref": "#/properties/features/items" }
                        }
                    }
                }
            }
        }
    }
    """;

    JsonSchema _schema;
    JsonSchema _topLevel;
    JsonSchema _nested;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because()
    {
        _topLevel = _schema.GetSchemaForPropertyPath(new PropertyPath("features"));
        _nested = _schema.GetSchemaForPropertyPath(new PropertyPath("features.subFeatures"));
    }

    [Fact] void should_resolve_the_top_level_item_schema() => _topLevel.ActualProperties.Keys.ShouldContainOnly("id", "name", "subFeatures");
    [Fact] void should_resolve_the_nested_self_referential_item_schema() => _nested.ActualProperties.Keys.ShouldContainOnly("id", "name", "subFeatures");
    [Fact] void should_resolve_the_nested_id_to_a_guid() => _nested.ActualProperties["id"].Format.ShouldEqual("guid");
}
