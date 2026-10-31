// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_refining_converter_backed_default_only_properties : Specification
{
    readonly string[] _shapes =
    [
        """{"default":null,"type":"number","format":"decimal?"}""",
        """{"default":null,"type":"array","items":{"type":"string","format":"string","compliance":[{"metadataType":"PII","details":""}]}}""",
        """{"default":null,"type":"string","format":"timespan?","$comment":"TimeSpan","pattern":"^-?\\d+:\\d+:\\d+$"}""",
        """{"default":null,"type":"integer","format":"enum?","enum":[0,1],"x-enumNames":["First","Second"]}""",
        """{"default":null,"type":"object","properties":{"name":{"type":"string"},"child":{"type":"object","properties":{"id":{"type":"integer"}},"required":["id"]}},"required":["name"],"additionalProperties":false}""",
        """{"default":null,"type":"string","minLength":1,"maxLength":1}"""
    ];
    bool[] _compatible;
    bool[] _typedChanges;

    void Because()
    {
        var legacy = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"default":null}}}""");
        var precise = _shapes.Select(shape => JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"value\":" + shape + "}}")).ToArray();
        _compatible = precise.Select(legacy.IsCompatibleWith).ToArray();
        _typedChanges = precise.Select(schema => schema.IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"value":{"default":null,"type":"boolean"}}}"""))).ToArray();
    }

    [Fact] void should_accept_scalar_concepts() => _compatible[0].ShouldBeTrue();
    [Fact] void should_accept_enumerables_of_concepts() => _compatible[1].ShouldBeTrue();
    [Fact] void should_accept_timespan_concepts() => _compatible[2].ShouldBeTrue();
    [Fact] void should_accept_enum_concepts() => _compatible[3].ShouldBeTrue();
    [Fact] void should_accept_object_overrides() => _compatible[4].ShouldBeTrue();
    [Fact] void should_accept_character_concepts() => _compatible[5].ShouldBeTrue();
    [Fact] void should_not_tolerate_changes_to_already_typed_properties() => _typedChanges.ShouldEqual(new bool[_shapes.Length]);
}
