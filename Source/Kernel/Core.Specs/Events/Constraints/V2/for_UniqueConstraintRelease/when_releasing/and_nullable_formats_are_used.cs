// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_nullable_formats_are_used : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-2b-component-key");
    JsonSchema _schema;
    JsonObject _content;
    IReadOnlyList<UniqueConstraintComponent> _components;
    string _originalSchema;
    string _encoded;
    string _expected;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "number":{"type":"integer","format":"int32?"},
                "nil":{"type":"integer","format":"int32?"},
                "missing":{"type":"integer","format":"int32?"},
                "numbers":{"type":"array","items":{"type":"integer","format":"int32?"}},
                "nullNumbers":{"type":"array","items":{"type":"integer","format":"int32?"}},
                "emptyNumbers":{"type":"array","items":{"type":"integer","format":"int32?"}}}}
            """);
        _content = JsonNode.Parse("""{"number":7,"nil":null,"numbers":[7,null],"nullNumbers":null,"emptyNumbers":[]}""")!.AsObject();
        _originalSchema = _schema.Node.ToJsonString();
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
            new UniqueConstraintEventDefinition("event", ["number", "nil", "missing", "numbers", "nullNumbers", "emptyNumbers"]), _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        _expected = UniqueConstraintEncoder.Encode(_key,
            [new(7, "int32"), new(null, "int32"), new(null, "int32"), new(new int?[] { 7, null }, "int32"), new(null, "int32"), new(Array.Empty<int>(), "int32")]).AsT0.Value;
    }

    [Fact] void should_strip_nullable_suffixes_from_scalar_and_item_formats() => _components.All(component => component.Format == "int32").ShouldBeTrue();
    [Fact] void should_convert_the_present_scalar() => _components[0].Value.ShouldEqual(7);
    [Fact] void should_convert_the_present_array_element() => ((object?[])_components[3].Value!)[0].ShouldEqual(7);
    [Fact] void should_keep_array_null_positions() => ((object?[])_components[3].Value!)[1].ShouldBeNull();
    [Fact] void should_match_the_actual_encoder_for_nonnullable_formats() => _encoded.ShouldEqual(_expected);
    [Fact] void should_allow_the_encoder_to_decline_a_null_scalar() => UniqueConstraintEncoder.Encode(_key, [_components[1]]).HasValue.ShouldBeFalse();
    [Fact] void should_allow_the_encoder_to_decline_a_missing_scalar() => UniqueConstraintEncoder.Encode(_key, [_components[2]]).HasValue.ShouldBeFalse();
    [Fact] void should_allow_the_encoder_to_decline_a_null_array() => UniqueConstraintEncoder.Encode(_key, [_components[4]]).HasValue.ShouldBeFalse();
    [Fact] void should_allow_the_encoder_to_claim_an_empty_array() => UniqueConstraintEncoder.Encode(_key, [_components[5]]).HasValue.ShouldBeTrue();
    [Fact] void should_not_modify_declared_schema_formats() => _schema.Node.ToJsonString().ShouldEqual(_originalSchema);
}
