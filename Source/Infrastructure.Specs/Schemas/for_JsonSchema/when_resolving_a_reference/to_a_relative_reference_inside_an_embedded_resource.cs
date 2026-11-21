// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_a_relative_reference_inside_an_embedded_resource : Specification
{
    JsonSchema _resource;
    JsonSchema _schema;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://example.invalid/root",
          "properties":{"value":{"$ref":"https://example.invalid/a/"}},
          "$defs":{
            "resource":{"$id":"https://example.invalid/a/","properties":{
              "direct":{"$ref":"sibling"},
              "array":{"type":"array","items":{"$ref":"sibling"}},
              "dictionary":{"additionalProperties":{"$ref":"sibling"}},
              "all":{"allOf":[{"$ref":"sibling"}]},
              "one":{"oneOf":[{"$ref":"sibling"}]},
              "any":{"anyOf":[{"$ref":"sibling"}]}
            }},
            "sibling":{"$id":"https://example.invalid/a/sibling","type":"string","title":"scoped"}
          }
        }
        """);

    void Because() => _resource = _schema.Properties["value"].Reference!;

    [Fact] void should_resolve_property_references() => _resource.Properties["direct"].Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_resolve_item_references() => _resource.Properties["array"].Item?.Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_resolve_additional_property_references() => _resource.Properties["dictionary"].AdditionalPropertiesSchema?.Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_resolve_all_of_references() => _resource.Properties["all"].AllOf[0].Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_resolve_one_of_references() => _resource.Properties["one"].OneOf[0].Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_resolve_any_of_references() => _resource.Properties["any"].AnyOf[0].Reference?.Title.ShouldEqual("scoped");
    [Fact] void should_not_invent_compliance_metadata() => _schema.HasComplianceMetadata().ShouldBeFalse();
}
