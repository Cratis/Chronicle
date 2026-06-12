// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_a_definition_via_definitions : Specification
{
    const string Json = """
    {
        "type": "object",
        "definitions": {
            "Thing": { "type": "object", "properties": { "name": { "type": "string" } } }
        },
        "properties": { "thing": { "$ref": "#/definitions/Thing" } }
    }
    """;

    JsonSchema _schema;
    JsonSchema _resolved;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _resolved = _schema.ActualProperties["thing"].Reference;

    [Fact] void should_resolve_the_reference() => _resolved.ShouldNotBeNull();
    [Fact] void should_expose_the_referenced_properties() => _resolved.ActualProperties.Keys.ShouldContain("name");
}
