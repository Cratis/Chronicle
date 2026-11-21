// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_personal_resource_is_referenced_by_id : Specification
{
    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "type":"object","properties":{"value":{"$ref":"personal"}},
          "$defs":{"personal":{"$id":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}}
        }
        """);

    void Because() => _result = _schema.HasComplianceMetadata();

    [Fact] void should_detect_the_personal_resource_metadata() => _result.ShouldBeTrue();
}
