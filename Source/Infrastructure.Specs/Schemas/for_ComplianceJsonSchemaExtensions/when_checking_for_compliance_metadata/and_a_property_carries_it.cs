// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_a_property_carries_it : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": {
            "name": { "type": "string" },
            "socialSecurityNumber": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
        }
    }
    """;

    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _result = _schema.HasComplianceMetadata();

    [Fact] void should_recognize_the_compliance_metadata() => _result.ShouldBeTrue();
}
