// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concept_properties_with_pii : given.a_json_schema_generator_with_pii_support
{
    record Note(string Value) : ConceptAs<string>(Value);

    [PII("Concept details")]
    record PersonalNote(string Value) : ConceptAs<string>(Value);

    class Notes
    {
        [PII("Property details")]
        public Note PropertyOnly { get; init; }

        public PersonalNote TypeOnly { get; init; }

        [PII("Property details")]
        public PersonalNote Both { get; init; }

        [PII("Primitive details")]
        public string Primitive { get; init; }

        public Note Unprotected { get; init; }
    }

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Notes));

    [Fact] void should_include_property_metadata_on_the_concept() => _result.ActualProperties["propertyOnly"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Property details"));
    [Fact] void should_preserve_type_metadata_on_the_concept() => _result.ActualProperties["typeOnly"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Concept details"));
    [Fact] void should_keep_type_metadata_first_without_duplicates_when_both_are_marked() => _result.ActualProperties["both"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Concept details"));
    [Fact] void should_preserve_primitive_property_metadata() => _result.ActualProperties["primitive"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Primitive details"));
    [Fact] void should_not_add_metadata_to_an_unprotected_concept() => _result.ActualProperties["unprotected"].HasComplianceMetadata().ShouldBeFalse();
}
