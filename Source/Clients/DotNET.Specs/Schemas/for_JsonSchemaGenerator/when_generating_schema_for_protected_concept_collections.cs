// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_protected_concept_collections : given.a_json_schema_generator_with_protection_support
{
    record Note(string Value) : ConceptAs<string>(Value);

    record Notes(
        [property: PII("Property details")] IReadOnlyList<Note> Personal,
        [property: Encrypted] Note[] Secrets,
        [PII("Parameter details")] IReadOnlyList<Note> PersonalParameter,
        [Encrypted] Note[] SecretParameter,
        [property: PII] IReadOnlyList<string> Primitive);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Notes));

    [Fact] void should_protect_the_pii_list_property() => _result.ActualProperties["Personal"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Property details"));
    [Fact] void should_protect_the_encrypted_array_property() => _result.ActualProperties["Secrets"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, ""));
    [Fact] void should_protect_the_pii_list_parameter() => _result.ActualProperties["PersonalParameter"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Parameter details"));
    [Fact] void should_protect_the_encrypted_array_parameter() => _result.ActualProperties["SecretParameter"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, ""));
    [Fact] void should_keep_the_same_node_type_as_primitive_collections() => _result.ActualProperties["Personal"].Type.ShouldEqual(_result.ActualProperties["Primitive"].Type);
    [Fact] void should_not_move_property_compliance_to_items() => _result.ActualProperties["Personal"].Item!.HasComplianceMetadata().ShouldBeFalse();
    [Fact] void should_not_move_property_security_to_items() => _result.ActualProperties["Secrets"].Item!.HasSecurityMetadata().ShouldBeFalse();
}
