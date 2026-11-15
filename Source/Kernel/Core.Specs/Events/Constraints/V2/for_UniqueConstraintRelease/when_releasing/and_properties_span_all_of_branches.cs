// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_properties_span_all_of_branches : Specification
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
            {"type":"object","allOf":[
                {"type":"object","properties":{"count":{"type":"integer","format":"int32"}}},
                {"type":"object","properties":{"quantity":{"type":"integer","format":"int64"}}},
                {"type":"object","properties":{"literal":{"type":"string"}}}]}
            """);
        _content = JsonNode.Parse("""{"count":7,"quantity":9223372036854775806,"literal":"{ABCDEF01-2345-6789-ABCD-EF0123456789}"}""")!.AsObject();
        _originalSchema = _schema.ToJson();
        _originalContent = _content.ToJsonString();
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
            new UniqueConstraintEventDefinition("event", ["quantity", "count", "literal"]), _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        _expected = UniqueConstraintEncoder.Encode(_key,
            [new(9223372036854775806L, "int64"), new(7, "int32"), new("{ABCDEF01-2345-6789-ABCD-EF0123456789}")]).AsT0.Value;
    }

    [Fact] void should_recognize_properties_in_every_branch() => _components.All(component => component.IsInSchema).ShouldBeTrue();
    [Fact] void should_preserve_the_first_branch_component_in_declaration_order() => _components[1].ShouldEqual(new UniqueConstraintComponent(7, "int32"));
    [Fact] void should_preserve_the_second_branch_integer_type_and_format() => _components[0].ShouldEqual(new UniqueConstraintComponent(9223372036854775806L, "int64"));
    [Fact] void should_keep_declared_formatless_text_as_a_string() => _components[2].Value.ShouldEqual("{ABCDEF01-2345-6789-ABCD-EF0123456789}");
    [Fact] void should_match_the_actual_encoder_for_typed_components() => _encoded.ShouldEqual(_expected);
    [Fact] void should_not_modify_the_schema() => _schema.ToJson().ShouldEqual(_originalSchema);
    [Fact] void should_not_modify_the_content() => _content.ToJsonString().ShouldEqual(_originalContent);
}
