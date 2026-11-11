// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_comparing_annotation_only_changes : Specification
{
    JsonSchema _plain;
    JsonSchema _annotated;
    bool _added;
    bool _removed;
    bool _equalWithAddedAnnotations;
    bool _equalWithRemovedAnnotations;

    void Establish()
    {
        _plain = JsonSchema.FromJson("""
            {"type":"object","properties":{"value":{"type":"string"},"untyped":{"default":null},"entries":{"type":"array","items":{"type":"string"}}},"$defs":{"Entry":{"type":"string"}},"allOf":[{"type":"object"}]}
            """);
        _annotated = JsonSchema.FromJson("""
            {
              "type":"object","title":"Event","description":"An event","deprecated":true,"contentEncoding":"base64","examples":[{}],"$comment":"Generated",
              "properties":{
                "value":{"type":"string","title":"Value","description":"A value","deprecated":true,"contentEncoding":"base64","examples":["value"],"$comment":"Generated"},
                "untyped":{"default":null,"description":"Legacy","deprecated":true,"contentEncoding":"base64","examples":[null],"$comment":"Generated"},
                "entries":{"type":"array","items":{"type":"string","description":"Entry","deprecated":true,"contentEncoding":"base64","examples":["entry"],"$comment":"Generated"}}
              },
              "$defs":{"Entry":{"type":"string","description":"Entry","deprecated":true,"contentEncoding":"base64","examples":["entry"],"$comment":"Generated"}},
              "allOf":[{"type":"object","description":"Composition","deprecated":true,"contentEncoding":"base64","examples":[{}],"$comment":"Generated"}]
            }
            """);
    }

    void Because()
    {
        _added = _plain.IsCompatibleWith(_annotated);
        _removed = _annotated.IsCompatibleWith(_plain);
        _equalWithAddedAnnotations = JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_plain.ToJson(), _annotated.ToJson());
        _equalWithRemovedAnnotations = JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_annotated.ToJson(), _plain.ToJson());
    }

    [Fact] void should_tolerate_added_annotations_at_schema_declarations() => _added.ShouldBeTrue();
    [Fact] void should_tolerate_removed_annotations_at_schema_declarations() => _removed.ShouldBeTrue();
    [Fact] void should_ignore_added_annotations_for_storage_equality() => _equalWithAddedAnnotations.ShouldBeTrue();
    [Fact] void should_ignore_removed_annotations_for_storage_equality() => _equalWithRemovedAnnotations.ShouldBeTrue();
}
