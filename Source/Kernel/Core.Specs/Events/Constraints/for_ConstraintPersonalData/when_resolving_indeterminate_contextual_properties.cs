// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintPersonalData;

public class when_resolving_indeterminate_contextual_properties : Specification
{
    IReadOnlyCollection<string> _properties;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
              "type":"object",
              "properties":{
                "dynamicUnavailable":{"$dynamicRef":"https://unavailable.example/schema#personal"},
                "recursiveInvalid":{"$recursiveRef":42},
                "contextualUnresolved":{"$dynamicRef":"#/$defs/unresolved"},
                "staticUnavailable":{"$ref":"https://unavailable.example/schema"},
                "plain":{"type":"string"}
              },
              "$defs":{"unresolved":{"$ref":"https://unavailable.example/schema"}}
            }
            """);
        _properties = ConstraintPersonalData.PropertiesOf(schema);
    }

    [Fact] void should_track_the_subject_for_unavailable_dynamic_references() => _properties.Contains("dynamicUnavailable").ShouldBeTrue();
    [Fact] void should_track_the_subject_for_invalid_recursive_references() => _properties.Contains("recursiveInvalid").ShouldBeTrue();
    [Fact] void should_track_the_subject_for_unresolved_edges_after_a_contextual_reference() => _properties.Contains("contextualUnresolved").ShouldBeTrue();
    [Fact] void should_preserve_unresolved_static_reference_behavior() => _properties.Contains("staticUnavailable").ShouldBeFalse();
    [Fact] void should_not_track_proven_plain_properties() => _properties.Contains("plain").ShouldBeFalse();
    [Fact] void should_track_only_the_three_indeterminate_properties() => _properties.Count.ShouldEqual(3);
}
