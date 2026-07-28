// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_resolved_reference : Specification
{
    const string Json = """
    {
        "type": "object",
        "$defs": {
            "Inner": { "type": "object", "title": "Inner", "properties": { "value": { "type": "integer" } } }
        },
        "properties": { "inner": { "$ref": "#/$defs/Inner" } }
    }
    """;

    JsonSchemaProperty _property;
    JsonSchema? _first;
    JsonSchema? _second;

    void Establish() => _property = JsonSchema.FromJson(Json).ActualProperties["inner"];

    void Because()
    {
        _first = _property.Reference;
        _second = _property.Reference;
    }

    [Fact] void should_resolve_the_reference() => _first!.Title.ShouldEqual("Inner");
    [Fact] void should_return_the_same_instance_on_repeated_access() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
