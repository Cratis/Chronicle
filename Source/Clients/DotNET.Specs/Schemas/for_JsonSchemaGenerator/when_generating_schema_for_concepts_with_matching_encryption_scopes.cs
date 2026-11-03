// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concepts_with_matching_encryption_scopes : given.a_json_schema_generator_with_protection_support
{
    [Encrypted(EncryptionScope.Namespace, "Type details")]
    record Secret(string Value) : ConceptAs<string>(Value);

    record Secrets([property: Encrypted(EncryptionScope.Namespace)] Secret Property, [Encrypted(EncryptionScope.Namespace)] Secret Parameter);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Secrets));

    [Fact] void should_include_the_matching_property_scope_once() => _result.ActualProperties["Property"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedNamespace.Value, "Type details"));
    [Fact] void should_include_the_matching_parameter_scope_once() => _result.ActualProperties["Parameter"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedNamespace.Value, "Type details"));
}
