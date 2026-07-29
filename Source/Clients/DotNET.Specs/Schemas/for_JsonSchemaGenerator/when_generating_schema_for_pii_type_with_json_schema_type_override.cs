// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// Substituting the schema of a type adorned with <see cref="JsonSchemaTypeAttribute"/> must not drop the
/// classification of the value it stands in for — the compliance metadata belongs to the value, not to the shape
/// it happens to serialize as, and losing it here would persist a <c>[PII]</c> value in the clear.
/// </summary>
public class when_generating_schema_for_pii_type_with_json_schema_type_override : given.a_json_schema_generator_with_pii_support
{
    [PII]
    [JsonSchemaType(typeof(string))]
    [JsonConverter(typeof(SocialSecurityNumberJsonConverter))]
    record SocialSecurityNumber(string Country, string Number);

    class SocialSecurityNumberJsonConverter : JsonConverter<SocialSecurityNumber>
    {
        public override SocialSecurityNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(string.Empty, reader.GetString() ?? string.Empty);

        public override void Write(Utf8JsonWriter writer, SocialSecurityNumber value, JsonSerializerOptions options) => writer.WriteStringValue($"{value.Country}-{value.Number}");
    }

    record Citizen(SocialSecurityNumber Identification);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Citizen));

    [Fact] void should_represent_the_adorned_type_with_the_declared_types_json_type() =>
        _result.ActualProperties["identification"].Type.ShouldEqual(JsonObjectType.String);

    [Fact] void should_have_compliance_metadata_on_the_substituted_schema() =>
        _result.ActualProperties["identification"].HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_have_pii_compliance_type_on_the_substituted_schema() =>
        _result.ActualProperties["identification"].GetComplianceMetadata()
            .Select(_ => _.metadataType)
            .ShouldContain(ComplianceMetadataType.PII.Value);
}
