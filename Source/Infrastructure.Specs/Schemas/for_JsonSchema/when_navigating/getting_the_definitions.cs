// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_navigating;

public class getting_the_definitions : Specification
{
    const string Json = """
    {
        "type": "object",
        "$defs": {
            "First": { "type": "object", "properties": { "a": { "type": "string" } } },
            "Second": { "type": "object", "properties": { "b": { "type": "string" } } }
        }
    }
    """;

    JsonSchema _schema;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    [Fact] void should_expose_all_definitions() => _schema.Definitions.Keys.ShouldContainOnly("First", "Second");
    [Fact] void should_expose_the_properties_of_a_definition() => _schema.Definitions["First"].ActualProperties.Keys.ShouldContain("a");
}
