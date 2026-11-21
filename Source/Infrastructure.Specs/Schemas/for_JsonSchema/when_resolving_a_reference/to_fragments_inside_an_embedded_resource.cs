// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_fragments_inside_an_embedded_resource : Specification
{
    JsonSchema _schema;
    JsonSchema _resource;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$id":"https://example.invalid/root",
          "Person":{"title":"legacy"},
          "properties":{"x":{"title":"root-member"},"rootOnly":{"title":"fallback"},"value":{"$ref":"a/"},"legacy":{"$ref":"#Person"}},
          "$defs":{
            "resource":{"$id":"a/","properties":{
              "x":{"title":"scoped-member"},
              "pointer":{"$ref":"#/properties/x"},
              "anchor":{"$ref":"#Person"},
              "fallback":{"$ref":"#/properties/rootOnly"},
              "slashless":{"$ref":"#properties/rootOnly"},
              "relativeFallback":{"$ref":"legacy"}
            },"$defs":{"person":{"$anchor":"Person","title":"scoped-anchor"}}},
            "legacy":{"$id":"legacy","title":"root-resource"}
          }
        }
        """);

    void Because() => _resource = _schema.Properties["value"].Reference!;

    [Fact] void should_resolve_a_pointer_inside_its_resource() => _resource.Properties["pointer"].Reference?.Title.ShouldEqual("scoped-member");
    [Fact] void should_resolve_an_anchor_inside_its_resource_before_a_root_collision() => _resource.Properties["anchor"].Reference?.Title.ShouldEqual("scoped-anchor");
    [Fact] void should_keep_root_pointer_fallback() => _resource.Properties["fallback"].Reference?.Title.ShouldEqual("fallback");
    [Fact] void should_keep_slashless_root_pointer_fallback() => _resource.Properties["slashless"].Reference?.Title.ShouldEqual("fallback");
    [Fact] void should_keep_root_resource_fallback() => _resource.Properties["relativeFallback"].Reference?.Title.ShouldEqual("root-resource");
    [Fact] void should_keep_the_legacy_root_anchor_collision() => _schema.Properties["legacy"].Reference?.Title.ShouldEqual("legacy");
    [Fact] void should_not_invent_metadata_for_fragments() => _resource.HasComplianceMetadata().ShouldBeFalse();
}
