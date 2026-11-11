// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_refining_annotated_properties : Specification
{
    static readonly (string Keyword, string Value)[] _annotations =
    [
        ("title", "\"Amount\""),
        ("description", "\"An amount\""),
        ("deprecated", "true"),
        ("contentEncoding", "\"base64\""),
        ("examples", "[1]"),
        ("$comment", "\"Generated\"")
    ];
    JsonSchema _number;
    JsonSchema _typed;
    JsonSchema _legacy;
    JsonSchema[] _annotatedNumbers;
    JsonSchema[] _annotatedTyped;
    JsonSchema[] _annotatedLegacy;
    bool[] _compatibleNumbers;
    bool[] _compatibleAnnotatedTyped;
    bool[] _compatibleAnnotatedLegacy;
    string[] _retainedTyped;
    string[] _retainedAnnotations;

    void Establish()
    {
        _number = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"number"}}}""");
        _typed = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{"type":"number","format":"decimal","default":null,"compliance":[{"metadataType":"PII","details":""}],"security":[{"metadataType":"Encrypted","details":""}]}}}
            """);
        _legacy = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"default":null}}}""");
        _annotatedNumbers = _annotations.Select(annotation => AddAnnotation(_number, annotation)).ToArray();
        _annotatedTyped = _annotations.Select(annotation => AddAnnotation(_typed, annotation)).ToArray();
        _annotatedLegacy = _annotations.Select(annotation => AddAnnotation(_legacy, annotation)).ToArray();
    }

    void Because()
    {
        _compatibleNumbers = _annotatedNumbers.Select(schema => _number.IsCompatibleWith(schema) && schema.IsCompatibleWith(_number)).ToArray();
        _compatibleAnnotatedTyped = _annotatedTyped.Select(schema => schema.IsCompatibleWith(_legacy) && _legacy.IsCompatibleWith(schema)).ToArray();
        _compatibleAnnotatedLegacy = _annotatedLegacy.Select(schema => _typed.IsCompatibleWith(schema) && schema.IsCompatibleWith(_typed)).ToArray();
        _retainedTyped = _annotatedLegacy.Select(schema => _typed.MorePrecise(schema).ToJson()).ToArray();
        _retainedAnnotations = _annotatedTyped.Select(schema => schema.MorePrecise(_legacy).ToJson()).ToArray();
    }

    [Fact] void should_tolerate_annotations_on_number_properties_in_both_directions() => _compatibleNumbers.ShouldEqual(Enumerable.Repeat(true, _annotations.Length).ToArray());
    [Fact] void should_tolerate_annotations_on_typed_refinements_in_both_directions() => _compatibleAnnotatedTyped.ShouldEqual(Enumerable.Repeat(true, _annotations.Length).ToArray());
    [Fact] void should_tolerate_annotations_on_legacy_properties_in_both_directions() => _compatibleAnnotatedLegacy.ShouldEqual(Enumerable.Repeat(true, _annotations.Length).ToArray());
    [Fact] void should_retain_stored_structure_and_protection() => _retainedTyped.ShouldEqual(Enumerable.Repeat(_typed.ToJson(), _annotations.Length).ToArray());
    [Fact] void should_retain_stored_annotations_without_mutation() => _retainedAnnotations.ShouldEqual(_annotatedTyped.Select(schema => schema.ToJson()).ToArray());

    static JsonSchema AddAnnotation(JsonSchema schema, (string Keyword, string Value) annotation)
    {
        var node = JsonNode.Parse(schema.ToJson())!.AsObject();
        node["properties"]!["value"]![annotation.Keyword] = JsonNode.Parse(annotation.Value);
        return new(node);
    }
}
