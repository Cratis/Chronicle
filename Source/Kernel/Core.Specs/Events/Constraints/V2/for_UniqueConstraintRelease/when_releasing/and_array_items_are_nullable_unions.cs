// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_array_items_are_nullable_unions : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-2b-component-key");
    JsonSchema _schema;
    JsonObject _content;
    IReadOnlyList<UniqueConstraintComponent> _components;
    string _originalSchema;
    string _originalContent;
    string _encoded;
    string _expected;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "days":{"type":"array","items":{"oneOf":[{"type":"string","format":"date"},{"type":"null"}]}},
                "nullableDays":{"type":"array","items":{"oneOf":[{"type":"null"},{"type":"string","format":"date?"}]}}}}
            """);
        _content = JsonNode.Parse("""{"days":["2025-02-03",null,"2026-10-25"],"nullableDays":[null,"2025-02-03"]}""")!.AsObject();
        _originalSchema = _schema.Node.ToJsonString();
        _originalContent = _content.ToJsonString();
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
            new UniqueConstraintEventDefinition("event", ["days", "nullableDays"]), _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        _expected = UniqueConstraintEncoder.Encode(_key,
        [
            new(new DateOnly?[] { new(2025, 2, 3), null, new(2026, 10, 25) }, "date"),
            new(new DateOnly?[] { null, new(2025, 2, 3) }, "date")
        ]).AsT0.Value;
    }

    [Fact] void should_extract_the_resolved_element_format() => _components[0].Format.ShouldEqual("date");
    [Fact] void should_convert_date_union_elements_to_date_only() => ((object?[])_components[0].Value!)[0].ShouldEqual(new DateOnly(2025, 2, 3));
    [Fact] void should_keep_null_elements_in_order() => ((object?[])_components[0].Value!)[1].ShouldBeNull();
    [Fact] void should_preserve_the_last_date() => ((object?[])_components[0].Value!)[2].ShouldEqual(new DateOnly(2026, 10, 25));
    [Fact] void should_resolve_a_union_with_null_first_and_a_nullable_format() => _components[1].Format.ShouldEqual("date");
    [Fact] void should_match_the_actual_encoder_for_typed_date_arrays() => _encoded.ShouldEqual(_expected);
    [Fact] void should_not_modify_the_original_item_schema() => _schema.Node.ToJsonString().ShouldEqual(_originalSchema);
    [Fact] void should_not_modify_source_content() => _content.ToJsonString().ShouldEqual(_originalContent);
}
