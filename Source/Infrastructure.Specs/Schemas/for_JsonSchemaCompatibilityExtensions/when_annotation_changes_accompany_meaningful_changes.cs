// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_annotation_changes_accompany_meaningful_changes : Specification
{
    static readonly (string Keyword, string? Value)[] _changes =
    [
        ("type", "\"integer\""),
        ("format", "\"int32\""),
        ("default", "2"),
        ("enum", "[1,3]"),
        ("minimum", "2"),
        ("compliance", null),
        ("security", null),
        ("readOnly", "true"),
        ("writeOnly", "true")
    ];
    JsonSchema _stored;
    JsonSchema[] _incoming;
    bool[] _compatible;
    bool[] _equal;

    void Establish()
    {
        _stored = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{"type":"number","format":"double","default":1,"enum":[1,2],"minimum":1,"compliance":[{"metadataType":"PII","details":""}],"security":[{"metadataType":"Encrypted","details":""}]}}}
            """);
        _incoming = _changes.Select(ChangeProperty).ToArray();
    }

    void Because()
    {
        _compatible = _incoming.Select(_stored.IsCompatibleWith).ToArray();
        _equal = _incoming.Select(schema => JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_stored.ToJson(), schema.ToJson())).ToArray();
    }

    [Fact] void should_reject_structural_and_protection_changes() => _compatible.ShouldEqual(new bool[_changes.Length]);
    [Fact] void should_not_hide_meaningful_changes_from_storage() => _equal.ShouldEqual(new bool[_changes.Length]);

    JsonSchema ChangeProperty((string Keyword, string? Value) change)
    {
        var node = JsonNode.Parse(_stored.ToJson())!.AsObject();
        var property = node["properties"]!["value"]!.AsObject();
        property["description"] = "A value";
        property["deprecated"] = true;
        if (change.Value is null)
        {
            property.Remove(change.Keyword);
        }
        else
        {
            property[change.Keyword] = JsonNode.Parse(change.Value);
        }
        return new(node);
    }
}
