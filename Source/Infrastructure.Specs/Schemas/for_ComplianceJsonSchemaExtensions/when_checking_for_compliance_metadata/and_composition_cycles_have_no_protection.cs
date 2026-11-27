// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_composition_cycles_have_no_protection : Specification
{
    JsonSchemaProperty _property;
    JsonSchema.MetadataReach _reach;

    void Establish() => _property = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$dynamicRef":"#value"}},
          "$defs":{"base":{"$dynamicAnchor":"value","allOf":[{"$ref":"#/$defs/base"}],"anyOf":[{"$ref":"#/$defs/base"}],"oneOf":[{"$ref":"#/$defs/base"}]}}
        }
        """).Properties["value"];

    void Because() => _reach = _property.MetadataReferences;

    [Fact] void should_visit_each_composition_node_once() => _reach.UniqueNodes.ShouldEqual(5);
    [Fact] void should_expand_the_anchor_group_once() => _reach.AnchorExpansions.ShouldEqual(1);
    [Fact] void should_finish_the_cycle() => _reach.Indeterminate.ShouldBeFalse();
    [Fact] void should_prove_compliance_absent() => _property.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_not_invent_a_metadata_type() => _property.GetComplianceMetadata().ShouldBeEmpty();
}
