// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_annotation_names_occur_in_default_payloads : Specification
{
    static readonly string[] _names = ["title", "description", "deprecated", "contentEncoding", "examples", "$comment"];
    JsonSchema _stored;
    JsonSchema[] _incoming;
    bool[] _compatible;
    bool[] _equal;

    void Establish()
    {
        _stored = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{"type":"object","default":{"title":"old","description":"old","deprecated":false,"contentEncoding":"old","examples":["old"],"$comment":"old"}}}}
            """);
        _incoming = _names.Select(ChangeDefault).ToArray();
    }

    void Because()
    {
        _compatible = _incoming.Select(_stored.IsCompatibleWith).ToArray();
        _equal = _incoming.Select(schema => JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_stored.ToJson(), schema.ToJson())).ToArray();
    }

    [Fact] void should_reject_changes_to_payload_data() => _compatible.ShouldEqual(new bool[_names.Length]);
    [Fact] void should_not_hide_default_changes_from_storage() => _equal.ShouldEqual(new bool[_names.Length]);

    JsonSchema ChangeDefault(string name)
    {
        var node = JsonNode.Parse(_stored.ToJson())!.AsObject();
        node["properties"]!["value"]!["default"]![name] = "new";
        return new(node);
    }
}
