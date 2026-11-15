// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_removing_properties_named_after_annotations : Specification
{
    static readonly string[] _names = ["title", "description", "deprecated", "examples", "$comment"];
    JsonSchema _stored;
    JsonSchema[] _incoming;
    bool[] _compatible;
    bool[] _equal;

    void Establish()
    {
        _stored = JsonSchema.FromJson("""
            {"type":"object","properties":{"nested":{"type":"object","properties":{"title":{"type":"string"},"description":{"type":"string"},"deprecated":{"type":"boolean"},"examples":{"type":"array"},"$comment":{"type":"string"}}}}}
            """);
        _incoming = _names.Select(RemoveProperty).ToArray();
    }

    void Because()
    {
        _compatible = _incoming.Select(_stored.IsCompatibleWith).ToArray();
        _equal = _incoming.Select(schema => JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_stored.ToJson(), schema.ToJson())).ToArray();
    }

    [Fact] void should_reject_removing_a_payload_property() => _compatible.ShouldEqual(new bool[_names.Length]);
    [Fact] void should_not_hide_removed_properties_from_storage() => _equal.ShouldEqual(new bool[_names.Length]);

    JsonSchema RemoveProperty(string name)
    {
        var node = JsonNode.Parse(_stored.ToJson())!.AsObject();
        node["properties"]!["nested"]!["properties"]!.AsObject().Remove(name);
        return new(node);
    }
}
