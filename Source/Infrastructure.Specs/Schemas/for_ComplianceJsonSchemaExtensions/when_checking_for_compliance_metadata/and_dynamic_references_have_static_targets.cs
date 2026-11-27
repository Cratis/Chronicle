// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_dynamic_references_have_static_targets : Specification
{
    JsonSchema _schema;
    bool _pointer;
    bool _ordinaryAnchor;
    bool _escapedPointer;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "$dynamicAnchor":"personal","compliance":[{"metadataType":"PII","details":""}],
          "properties":{
            "pointer":{"$dynamicRef":"#/$defs/plain"},
            "ordinary":{"$dynamicRef":"#ordinary"},
            "escaped":{"$dynamicRef":"#/$defs/a~1b~0c"}
          },
          "$defs":{
            "plain":{"type":"string"},
            "ordinary":{"$anchor":"ordinary","type":"string"},
            "a/b~c":{"type":"string","security":[{"metadataType":"Secret","details":""}]}
          }
        }
        """);

    void Because()
    {
        _pointer = _schema.Properties["pointer"].HasComplianceMetadata();
        _ordinaryAnchor = _schema.Properties["ordinary"].HasComplianceMetadata();
        _escapedPointer = _schema.Properties["escaped"].HasSecurityMetadata();
    }

    [Fact] void should_not_rebind_a_pointer_to_an_outer_anchor() => _pointer.ShouldBeFalse();
    [Fact] void should_not_rebind_an_ordinary_anchor() => _ordinaryAnchor.ShouldBeFalse();
    [Fact] void should_resolve_escaped_pointer_tokens() => _escapedPointer.ShouldBeTrue();
}
