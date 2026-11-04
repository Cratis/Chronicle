// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concept_properties_with_encryption : given.a_json_schema_generator
{
    record Secret(string Value) : ConceptAs<string>(Value);

    class Secrets
    {
        [Encrypted(details: "Subject details")]
        public Secret Subject { get; init; }

        [Encrypted(EncryptionScope.Namespace, "Namespace details")]
        public Secret Namespace { get; init; }

        [Encrypted(EncryptionScope.Global, "Global details")]
        public Secret Global { get; init; }

        [Encrypted(details: "Primitive details")]
        public string Primitive { get; init; }

        public Secret Unprotected { get; init; }
    }

    JsonSchema _result;

    void Establish()
    {
        var provider = new EncryptedMetadataProvider();
        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new SecurityMetadataResolver(
                new KnownInstancesOf<ICanProvideSecurityMetadataForType>([provider]),
                new KnownInstancesOf<ICanProvideSecurityMetadataForProperty>([provider])),
            _namingPolicy);
    }

    void Because() => _result = _generator.Generate(typeof(Secrets));

    [Fact] void should_include_subject_scoped_property_metadata() => _result.ActualProperties["Subject"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, "Subject details"));
    [Fact] void should_include_namespace_scoped_property_metadata() => _result.ActualProperties["Namespace"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedNamespace.Value, "Namespace details"));
    [Fact] void should_include_global_scoped_property_metadata() => _result.ActualProperties["Global"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedGlobal.Value, "Global details"));
    [Fact] void should_preserve_primitive_property_metadata() => _result.ActualProperties["Primitive"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, "Primitive details"));
    [Fact] void should_not_add_metadata_to_an_unprotected_concept() => _result.ActualProperties["Unprotected"].HasSecurityMetadata().ShouldBeFalse();
    [Fact] void should_not_classify_encryption_as_compliance() => _result.HasComplianceMetadata().ShouldBeFalse();
}
