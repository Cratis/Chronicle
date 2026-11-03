// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concept_collections_with_mixed_protection : given.a_json_schema_generator_with_protection_support
{
    [PII]
    record Personal(string Value) : ConceptAs<string>(Value);

    [Encrypted]
    record Secret(string Value) : ConceptAs<string>(Value);

    record EncryptedPersonal([property: Encrypted] IReadOnlyList<Personal> Values);
    record PersonalSecret([property: PII] Secret[] Values);

    Exception _encryptedPersonalError;
    Exception _personalSecretError;

    void Because()
    {
        _encryptedPersonalError = Catch.Exception(() => _generator.Generate(typeof(EncryptedPersonal)));
        _personalSecretError = Catch.Exception(() => _generator.Generate(typeof(PersonalSecret)));
    }

    [Fact] void should_reject_encryption_on_a_pii_list() => _encryptedPersonalError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
    [Fact] void should_reject_pii_on_an_encrypted_array() => _personalSecretError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
}
