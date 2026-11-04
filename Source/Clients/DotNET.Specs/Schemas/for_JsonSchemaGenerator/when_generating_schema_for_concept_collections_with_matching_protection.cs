// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concept_collections_with_matching_protection : given.a_json_schema_generator_with_protection_support
{
    [Encrypted(EncryptionScope.Namespace, "Concept details")]
    record Secret(string Value) : ConceptAs<string>(Value);

    [PII("Personal details")]
    record Personal(string Value) : ConceptAs<string>(Value);

    record Values(
        [property: Encrypted(EncryptionScope.Namespace)] Secret[] Secrets,
        [PII] IReadOnlyList<Personal> People);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Values));

    [Fact]
    void should_emit_matching_encryption_once() => _result.ActualProperties["Secrets"].GetSecurityMetadata()
        .Concat(_result.ActualProperties["Secrets"].Item!.GetSecurityMetadata()).Single()
        .ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedNamespace.Value, "Concept details"));

    [Fact]
    void should_emit_matching_pii_once() => _result.ActualProperties["People"].GetComplianceMetadata()
        .Concat(_result.ActualProperties["People"].Item!.GetComplianceMetadata()).Single()
        .ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Personal details"));
}
