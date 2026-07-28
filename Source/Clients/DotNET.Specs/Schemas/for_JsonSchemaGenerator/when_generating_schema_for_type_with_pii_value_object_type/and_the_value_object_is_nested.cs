// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_type_with_pii_value_object_type;

public class and_the_value_object_is_nested : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record VerifiedDateOfBirth(string DateOfBirth, string VerifiedBy);

    record ExpressVerification(string Id, VerifiedDateOfBirth DateOfBirth);

    JsonSchema _result;

    IReadOnlyDictionary<string, JsonSchemaProperty> NestedProperties => _result.ActualProperties["dateOfBirth"].ActualTypeSchema.ActualProperties;

    void Because() => _result = _generator.Generate(typeof(ExpressVerification));

    [Fact] void should_have_compliance_metadata() => _result.HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_push_compliance_metadata_onto_the_value_object_leaf() =>
        NestedProperties["dateOfBirth"].GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_push_compliance_metadata_onto_every_other_leaf_of_the_value_object() =>
        NestedProperties["verifiedBy"].GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_not_leave_compliance_metadata_on_the_value_object_container() =>
        _result.ActualProperties["dateOfBirth"].GetComplianceMetadata().ShouldBeEmpty();

    [Fact] void should_not_mark_unrelated_properties() =>
        _result.ActualProperties["id"].GetComplianceMetadata().ShouldBeEmpty();
}
