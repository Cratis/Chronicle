// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_type_with_pii_value_object_type;

public class and_the_leaves_are_enums : given.a_json_schema_generator_with_pii_support
{
    enum VerificationStatus
    {
        Unknown = 0,
        Verified = 1
    }

    [PII]
    record TypeLevelValue(VerificationStatus Status, VerificationStatus? OptionalStatus);

    record PropertyLevelValue(VerificationStatus Status);

    record EventWithEnumValues(
        TypeLevelValue TypeLevel,
        [property: PII] PropertyLevelValue PropertyLevel);

    JsonSchema _result;

    JsonSchemaProperty TypeLevelStatus =>
        _result.ActualProperties["typeLevel"].ActualTypeSchema.ActualProperties["status"];

    JsonSchemaProperty TypeLevelOptionalStatus =>
        _result.ActualProperties["typeLevel"].ActualTypeSchema.ActualProperties["optionalStatus"];

    JsonSchemaProperty PropertyLevelStatus =>
        _result.ActualProperties["propertyLevel"].ActualTypeSchema.ActualProperties["status"];

    void Because() => _result = _generator.Generate(typeof(EventWithEnumValues));

    [Fact] void should_push_type_level_pii_onto_the_enum_leaf() =>
        TypeLevelStatus.GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_push_type_level_pii_onto_the_nullable_enum_leaf() =>
        TypeLevelOptionalStatus.GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_push_property_level_pii_onto_the_enum_leaf() =>
        PropertyLevelStatus.GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);

    [Fact] void should_keep_the_plaintext_enum_schema() => TypeLevelStatus.ActualTypeSchema.Type.ShouldEqual(JsonObjectType.Integer);
}
