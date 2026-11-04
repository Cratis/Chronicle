// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_concepts_with_pii_and_encryption : given.a_json_schema_generator_with_protection_support
{
    [Encrypted]
    record Secret(string Value) : ConceptAs<string>(Value);

    [PII]
    record Personal(string Value) : ConceptAs<string>(Value);

    record PersonalSecret([property: PII] Secret Value);
    record EncryptedPersonal([property: Encrypted] Personal Value);

    Exception _personalSecretError;
    Exception _encryptedPersonalError;

    void Because()
    {
        _personalSecretError = Catch.Exception(() => _generator.Generate(typeof(PersonalSecret)));
        _encryptedPersonalError = Catch.Exception(() => _generator.Generate(typeof(EncryptedPersonal)));
    }

    [Fact] void should_reject_pii_on_an_encrypted_concept() => _personalSecretError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
    [Fact] void should_reject_encryption_on_a_pii_concept() => _encryptedPersonalError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
}
