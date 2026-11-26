// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_the_nearest_resource : Specification
{
    JsonSchema _schema;
    JsonSchema _nested;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://example.invalid/",
          "$defs":{
            "outer":{"$id":"a/","properties":{"value":{"$ref":"sibling"}},"$defs":{
              "nested":{"$id":"b/","properties":{"value":{"$ref":"sibling"}}},
              "innerSibling":{"$id":"b/sibling","title":"inner"},
              "outerSibling":{"$id":"sibling","title":"outer"}
            }}
          }
        }
        """);

    void Because() => _nested = _schema.Definitions["outer"].Definitions["nested"];

    [Fact] void should_use_the_enclosing_resource() => _schema.Definitions["outer"].Properties["value"].Reference?.Title.ShouldEqual("outer");
    [Fact] void should_use_the_nearest_nested_resource() => _nested.Properties["value"].Reference?.Title.ShouldEqual("inner");
    [Fact] void should_not_invent_metadata_in_nested_resources() => _nested.HasComplianceMetadata().ShouldBeFalse();
}
