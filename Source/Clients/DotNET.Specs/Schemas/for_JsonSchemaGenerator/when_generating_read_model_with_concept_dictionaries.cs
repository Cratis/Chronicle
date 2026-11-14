// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_read_model_with_concept_dictionaries : given.a_json_schema_generator_with_protection_support
{
    [PII("Login details")]
    record Login(string Value) : ConceptAs<string>(Value);

    [Encrypted]
    record Secret(string Value) : ConceptAs<string>(Value);

    record Identifier(Guid Value) : ConceptAs<Guid>(Value);

    record State(
        Dictionary<Guid, Login> PersonalDictionary,
        IDictionary<Guid, Login> PersonalInterface,
        IReadOnlyDictionary<Guid, Login> PersonalReadOnly,
        Dictionary<Guid, Secret> SecretDictionary,
        IDictionary<Guid, Secret> SecretInterface,
        IReadOnlyDictionary<Guid, Secret> SecretReadOnly,
        Dictionary<Guid, Identifier> PlainDictionary,
        IDictionary<Guid, Identifier> PlainInterface,
        IReadOnlyDictionary<Guid, Identifier> PlainReadOnly,
        [property: PII("Dictionary details")] IReadOnlyDictionary<Guid, Identifier> Classified);

    JsonSchema _result;

    void Because() => _result = _generator.GenerateForReadModel(typeof(State));

    [Fact] void should_type_pii_dictionary_values() => ValueSchemas("Personal").Select(_ => _.Type).ShouldEqual(Enumerable.Repeat(JsonObjectType.String, 3));
    [Fact] void should_type_encrypted_dictionary_values() => ValueSchemas("Secret").Select(_ => _.Type).ShouldEqual(Enumerable.Repeat(JsonObjectType.String, 3));
    [Fact] void should_type_plain_dictionary_values() => ValueSchemas("Plain").Select(_ => _.Type).ShouldEqual(Enumerable.Repeat(JsonObjectType.String, 3));
    [Fact] void should_preserve_plain_value_formats() => ValueSchemas("Plain").Select(_ => _.Format).ShouldEqual(Enumerable.Repeat("guid", 3));
    [Fact] void should_preserve_pii_value_metadata() => ValueSchemas("Personal").Select(_ => _.GetComplianceMetadata().Single()).ShouldEqual(Enumerable.Repeat(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Login details"), 3));
    [Fact] void should_preserve_encrypted_value_metadata() => ValueSchemas("Secret").Select(_ => _.GetSecurityMetadata().Single()).ShouldEqual(Enumerable.Repeat(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, ""), 3));
    [Fact] void should_not_classify_plain_values() => ValueSchemas("Plain").Select(_ => _.HasComplianceMetadata()).ShouldEqual(Enumerable.Repeat(false, 3));
    [Fact] void should_not_encrypt_plain_values() => ValueSchemas("Plain").Select(_ => _.HasSecurityMetadata()).ShouldEqual(Enumerable.Repeat(false, 3));
    [Fact] void should_keep_property_metadata_on_the_dictionary() => _result.ActualProperties["Classified"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Dictionary details"));
    [Fact] void should_not_move_property_metadata_to_values() => _result.ActualProperties["Classified"].AdditionalPropertiesSchema.HasComplianceMetadata().ShouldBeFalse();

    IEnumerable<JsonSchema> ValueSchemas(string prefix) => _result.ActualProperties.Where(_ => _.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(_ => _.Value.AdditionalPropertiesSchema!);
}
