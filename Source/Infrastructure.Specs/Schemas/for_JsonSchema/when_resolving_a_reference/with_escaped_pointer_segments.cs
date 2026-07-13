// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

/// <summary>
/// JSON Pointer (RFC 6901) escapes '~' as '~0' and '/' as '~1'. The resolver must unescape segments.
/// </summary>
public class with_escaped_pointer_segments : Specification
{
    const string Json = """
    {
        "type": "object",
        "$defs": {
            "weird~name": { "type": "object", "properties": { "value": { "type": "string" } } }
        },
        "properties": { "thing": { "$ref": "#/$defs/weird~0name" } }
    }
    """;

    JsonSchema _schema;
    JsonSchema _resolved;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _resolved = _schema.ActualProperties["thing"].Reference;

    [Fact] void should_resolve_the_escaped_reference() => _resolved.ShouldNotBeNull();
    [Fact] void should_expose_the_referenced_properties() => _resolved.ActualProperties.Keys.ShouldContain("value");
}
