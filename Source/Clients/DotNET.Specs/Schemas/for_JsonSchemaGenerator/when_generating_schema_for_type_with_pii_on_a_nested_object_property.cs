// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_pii_on_a_nested_object_property : given.a_json_schema_generator_with_pii_support
{
    record VerifiedDateOfBirth(string DateOfBirth, string VerifiedBy);

    record ExpressVerification(string Id, [property: PII] VerifiedDateOfBirth DateOfBirth);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(ExpressVerification));

    [Fact] void should_have_compliance_metadata() => _result.HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_push_compliance_metadata_onto_the_nested_leaves() =>
        _result.ActualProperties["dateOfBirth"].ActualTypeSchema.ActualProperties["dateOfBirth"]
            .GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_not_leave_compliance_metadata_on_the_object_container() =>
        _result.ActualProperties["dateOfBirth"].GetComplianceMetadata().ShouldBeEmpty();
}
