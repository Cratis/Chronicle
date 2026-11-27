// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class with_contextual_keywords_only : Specification
{
    JsonSchemaProperty _unresolved;
    JsonSchemaProperty _invalid;
    JsonSchemaProperty _resolved;
    JsonSchemaProperty _ordinary;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
              "properties":{
                "unresolved":{"$dynamicRef":"https://unavailable.example/schema#value"},
                "invalid":{"$recursiveRef":42},
                "resolved":{"$dynamicRef":"#/$defs/text"},
                "ordinary":{"$ref":"#/$defs/text"}
              },
              "$defs":{"text":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}
            }
            """);
        _unresolved = schema.Properties["unresolved"];
        _invalid = schema.Properties["invalid"];
        _resolved = schema.Properties["resolved"];
        _ordinary = schema.Properties["ordinary"];
    }

    [Fact] void should_not_expose_an_unresolved_contextual_reference_as_an_ordinary_reference() => _unresolved.HasReference.ShouldBeFalse();
    [Fact] void should_not_expose_an_invalid_contextual_reference_as_an_ordinary_reference() => _invalid.HasReference.ShouldBeFalse();
    [Fact] void should_not_expose_a_resolved_contextual_reference_as_an_ordinary_reference() => _resolved.HasReference.ShouldBeFalse();
    [Fact] void should_leave_the_unresolved_reference_null() => _unresolved.Reference.ShouldBeNull();
    [Fact] void should_leave_the_invalid_reference_null() => _invalid.Reference.ShouldBeNull();
    [Fact] void should_leave_the_contextual_target_out_of_ordinary_navigation() => _resolved.Reference.ShouldBeNull();
    [Fact] void should_keep_unresolved_type_navigation_unknown() => _unresolved.ActualTypeSchema.Type.ShouldEqual(JsonObjectType.None);
    [Fact] void should_keep_invalid_type_navigation_unknown() => _invalid.ActualTypeSchema.Type.ShouldEqual(JsonObjectType.None);
    [Fact] void should_keep_resolved_contextual_type_navigation_unknown() => _resolved.ActualTypeSchema.Type.ShouldEqual(JsonObjectType.None);
    [Fact] void should_still_classify_contextual_metadata() => _resolved.HasComplianceMetadata().ShouldBeTrue();
    [Fact] void should_keep_ordinary_reference_detection() => _ordinary.HasReference.ShouldBeTrue();
    [Fact] void should_keep_ordinary_reference_resolution() => _ordinary.Reference!.Type.ShouldEqual(JsonObjectType.String);
}
