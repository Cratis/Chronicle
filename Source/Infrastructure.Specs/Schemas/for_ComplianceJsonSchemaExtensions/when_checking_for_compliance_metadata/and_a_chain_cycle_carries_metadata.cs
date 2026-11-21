// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_chain_cycle_carries_metadata : Specification
{
    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson("""
        {"$ref":"#/$defs/A","$defs":{
          "A":{"$ref":"#/$defs/B"},
          "B":{"$ref":"#/$defs/A","compliance":[{"metadataType":"PII","details":""}]}
        }}
        """);

    void Because() => _result = _schema.HasComplianceMetadata();

    [Fact] void should_detect_metadata_before_terminating_the_cycle() => _result.ShouldBeTrue();
}
