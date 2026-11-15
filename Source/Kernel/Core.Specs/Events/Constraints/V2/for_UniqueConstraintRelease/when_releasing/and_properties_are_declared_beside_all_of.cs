// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_properties_are_declared_beside_all_of : Specification
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
            {"type":"object","properties":{"quantity":{"type":"integer","format":"int64"}},
             "allOf":[{"type":"object","properties":{"count":{"type":"integer","format":"int32"}}}]}
            """);
        _content = JsonNode.Parse("""{"quantity":9223372036854775806,"count":7}""")!.AsObject();
        _originalSchema = _schema.ToJson();
        _originalContent = _content.ToJsonString();
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
            new UniqueConstraintEventDefinition("event", ["quantity", "count"]), _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        _expected = UniqueConstraintEncoder.Encode(_key, [new(9223372036854775806L, "int64"), new(7, "int32")]).AsT0.Value;
    }

    [Fact] void should_recognize_both_direct_and_composed_properties() => _components.All(component => component.IsInSchema).ShouldBeTrue();
    [Fact] void should_preserve_the_direct_integer_type_and_format() => _components[0].ShouldEqual(new UniqueConstraintComponent(9223372036854775806L, "int64"));
    [Fact] void should_preserve_the_composed_integer_type_and_format() => _components[1].ShouldEqual(new UniqueConstraintComponent(7, "int32"));
    [Fact] void should_match_the_actual_encoder_for_typed_components() => _encoded.ShouldEqual(_expected);
    [Fact] void should_not_modify_the_schema() => _schema.ToJson().ShouldEqual(_originalSchema);
    [Fact] void should_not_modify_the_content() => _content.ToJsonString().ShouldEqual(_originalContent);
}
