// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_pii_adorned_type : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record PersonalData(string Name, string SocialSecurityNumber);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(PersonalData));

    [Fact] void should_have_compliance_metadata_on_schema() =>
        _result.HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_have_pii_compliance_type_on_schema() =>
        _result.GetComplianceMetadata()
            .Select(_ => _.metadataType)
            .ShouldContain(ComplianceMetadataType.PII.Value);
}
