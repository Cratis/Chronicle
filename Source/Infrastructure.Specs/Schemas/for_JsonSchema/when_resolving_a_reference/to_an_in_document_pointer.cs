// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

/// <summary>
/// System.Text.Json emits recurring/self-referential types as in-document JSON Pointers that point at the
/// first occurrence of the type (e.g. "#/properties/features/items") rather than into "$defs". Resolving
/// such pointers is required for self-referential read models to project correctly.
/// </summary>
public class to_an_in_document_pointer : Specification
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
    JsonSchema _resolved;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _resolved = _schema
        .ActualProperties["features"].Item
        .ActualProperties["subFeatures"].Item
        .Reference;

    [Fact] void should_resolve_the_self_referential_pointer() => _resolved.ShouldNotBeNull();
    [Fact] void should_expose_the_referenced_object_properties() => _resolved.ActualProperties.Keys.ShouldContainOnly("id", "subFeatures");
}
