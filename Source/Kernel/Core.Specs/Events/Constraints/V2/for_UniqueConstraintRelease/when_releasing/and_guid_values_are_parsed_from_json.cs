// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_guid_values_are_parsed_from_json : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-2b-component-key");
    readonly Guid _guid = new("abcdef01-2345-6789-abcd-ef0123456789");
    JsonSchema _schema;
    JsonObject _content;
    IReadOnlyList<UniqueConstraintComponent> _components;
    string _originalContent;
    string _originalSchema;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "identifier":{"type":"string","format":"guid"},
                "alias":{"type":"string","format":"uuid"},
                "nullable":{"type":"string","format":"guid?"},
                "identifiers":{"type":"array","items":{"oneOf":[{"type":"null"},{"type":"string","format":"uuid?"}]}}}}
            """);
        _content = JsonNode.Parse("""
            {"identifier":"abcdef01-2345-6789-abcd-ef0123456789",
             "alias":"{ABCDEF01-2345-6789-ABCD-EF0123456789}",
             "nullable":"ABCDEF01-2345-6789-ABCD-EF0123456789",
             "identifiers":[null,"{ABCDEF01-2345-6789-ABCD-EF0123456789}"]}
            """)!.AsObject();
        _originalContent = _content.ToJsonString();
        _originalSchema = _schema.ToJson();
    }

    void Because() => _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
        new UniqueConstraintEventDefinition("event", ["identifier", "alias", "nullable", "identifiers"]), _content, _schema);

    [Fact] void should_convert_the_guid_string_to_a_guid() => _components[0].Value.ShouldBeOfExactType<Guid>();
    [Fact] void should_preserve_the_guid_value() => _components[0].Value.ShouldEqual(_guid);
    [Fact] void should_parse_the_uuid_alias_with_braces() => _components[1].Value.ShouldEqual(_guid);
    [Fact] void should_parse_the_nullable_guid_format() => _components[2].Value.ShouldEqual(_guid);
    [Fact] void should_preserve_array_null_positions() => ((object?[])_components[3].Value!)[0].ShouldBeNull();
    [Fact] void should_parse_nullable_uuid_union_items() => ((object?[])_components[3].Value!)[1].ShouldEqual(_guid);
    [Fact] void should_not_modify_the_persisted_content() => _content.ToJsonString().ShouldEqual(_originalContent);
    [Fact] void should_not_modify_the_schema() => _schema.ToJson().ShouldEqual(_originalSchema);
    [Fact] void should_encode_the_same_as_typed_guid_components() =>
        UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value.ShouldEqual(
            UniqueConstraintEncoder.Encode(_key,
                [new(_guid, "guid"), new(_guid, "uuid"), new(_guid, "guid"), new(new Guid?[] { null, _guid }, "uuid")]).AsT0.Value);
}
