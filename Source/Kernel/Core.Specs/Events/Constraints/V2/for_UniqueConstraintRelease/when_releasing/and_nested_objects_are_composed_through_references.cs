// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_nested_objects_are_composed_through_references : Specification
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
            {"type":"object","properties":{"profile":{"oneOf":[{"type":"null"},{"$ref":"#/$defs/profile"}]}},
             "$defs":{
                "profile":{"type":"object","properties":{"price":{"type":"number","format":"decimal"}},"allOf":[
                    {"$ref":"#/$defs/base"},
                    {"type":"object","properties":{"details":{"oneOf":[{"$ref":"#/$defs/details"},{"type":"null"}]}}}]},
                "base":{"type":"object","properties":{"quantity":{"type":"integer","format":"int64"}}},
                "details":{"type":"object","allOf":[
                    {"type":"object","properties":{"day":{"type":"string","format":"date"}}},
                    {"type":"object","properties":{"days":{"type":"array","items":{"oneOf":[{"type":"null"},{"type":"string","format":"date?"}]}}}}]}}}
            """);
        _content = JsonNode.Parse("""
            {"profile":{"quantity":9223372036854775806,"price":12.3400,"details":{"day":"2025-02-03","days":[null,"2025-02-03"]}}}
            """)!.AsObject();
        _originalSchema = _schema.ToJson();
        _originalContent = _content.ToJsonString();
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
            new UniqueConstraintEventDefinition("event", ["PROFILE.Quantity", "profile.price", "profile.details.day", "profile.details.days"]), _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        _expected = UniqueConstraintEncoder.Encode(_key,
        [
            new(9223372036854775806L, "int64"), new(12.3400m, "decimal"),
            new(new DateOnly(2025, 2, 3), "date"), new(new DateOnly?[] { null, new(2025, 2, 3) }, "date")
        ]).AsT0.Value;
    }

    [Fact] void should_recognize_every_nested_property() => _components.All(component => component.IsInSchema).ShouldBeTrue();
    [Fact] void should_resolve_inherited_properties_case_insensitively() => _components[0].ShouldEqual(new UniqueConstraintComponent(9223372036854775806L, "int64"));
    [Fact] void should_preserve_properties_beside_nested_composition() => _components[1].ShouldEqual(new UniqueConstraintComponent(12.3400m, "decimal"));
    [Fact] void should_resolve_composed_objects_inside_nullable_reference_unions() => _components[2].ShouldEqual(new UniqueConstraintComponent(new DateOnly(2025, 2, 3), "date"));
    [Fact] void should_preserve_the_nullable_array_item_format() => _components[3].Format.ShouldEqual("date");
    [Fact] void should_keep_array_null_positions() => ((object?[])_components[3].Value!)[0].ShouldBeNull();
    [Fact] void should_convert_nullable_array_items() => ((object?[])_components[3].Value!)[1].ShouldEqual(new DateOnly(2025, 2, 3));
    [Fact] void should_match_the_actual_encoder_for_typed_components() => _encoded.ShouldEqual(_expected);
    [Fact] void should_not_modify_the_schema() => _schema.ToJson().ShouldEqual(_originalSchema);
    [Fact] void should_not_modify_the_content() => _content.ToJsonString().ShouldEqual(_originalContent);
}
