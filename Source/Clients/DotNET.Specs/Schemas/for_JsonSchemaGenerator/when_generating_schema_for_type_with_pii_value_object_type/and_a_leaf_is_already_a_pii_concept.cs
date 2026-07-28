// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_type_with_pii_value_object_type;

public class and_a_leaf_is_already_a_pii_concept : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record DateOfBirth(string Value) : ConceptAs<string>(Value);

    [PII]
    record VerifiedDateOfBirth(DateOfBirth DateOfBirth, string VerifiedBy);

    record ExpressVerification(string Id, VerifiedDateOfBirth DateOfBirth);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(ExpressVerification));

    [Fact] void should_record_the_metadata_only_once_on_the_doubly_marked_leaf() =>
        _result.ActualProperties["dateOfBirth"].ActualTypeSchema.ActualProperties["dateOfBirth"]
            .GetComplianceMetadata().Count().ShouldEqual(1);
}
