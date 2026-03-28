// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_pii_property : given.a_json_schema_generator_with_pii_support
{
    class TypeWithPIIProperty
    {
        public string Name { get; init; } = string.Empty;

        [PII]
        public string SocialSecurityNumber { get; init; } = string.Empty;
    }

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(TypeWithPIIProperty));

    [Fact] void should_have_compliance_metadata_on_pii_property() =>
        _result.ActualProperties["socialSecurityNumber"].HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_have_pii_compliance_type_on_pii_property() =>
        _result.ActualProperties["socialSecurityNumber"].GetComplianceMetadata()
            .Select(_ => _.metadataType)
            .ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_not_have_compliance_metadata_on_non_pii_property() =>
        _result.ActualProperties["name"].HasComplianceMetadata().ShouldBeFalse();
}
