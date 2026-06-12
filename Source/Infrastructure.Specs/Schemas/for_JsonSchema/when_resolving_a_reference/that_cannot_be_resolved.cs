// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class that_cannot_be_resolved : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": { "thing": { "$ref": "#/$defs/Missing" } }
    }
    """;

    JsonSchema _schema;
    JsonSchema? _resolved;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _resolved = _schema.ActualProperties["thing"].Reference;

    [Fact] void should_not_resolve() => _resolved.ShouldBeNull();
}
