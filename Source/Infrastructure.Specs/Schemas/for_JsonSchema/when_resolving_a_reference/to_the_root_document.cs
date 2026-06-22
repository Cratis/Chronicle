// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_the_root_document : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": {
            "name": { "type": "string" },
            "self": { "$ref": "#" }
        }
    }
    """;

    JsonSchema _schema;
    JsonSchema _resolved;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _resolved = _schema.ActualProperties["self"].Reference;

    [Fact] void should_resolve_to_the_root() => _resolved.ShouldNotBeNull();
    [Fact] void should_expose_the_root_properties() => _resolved.ActualProperties.Keys.ShouldContainOnly("name", "self");
}
