// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_pushing_conflicting_protection_to_nested_concepts : given.a_json_schema_generator_with_protection_support
{
    [Encrypted]
    record SubjectSecret(string Value) : ConceptAs<string>(Value);

    [PII]
    record Personal(string Value) : ConceptAs<string>(Value);

    record Inner(SubjectSecret S);
    record PersonalInner(Personal P);
    record Outer([property: Encrypted(EncryptionScope.Global)] Inner Inner);

    [Encrypted(EncryptionScope.Global)]
    record EncryptedOuter(Inner Inner);

    record PersonalOuter([property: PII] Inner Inner);
    record SecretOuter([property: Encrypted] PersonalInner Inner);

    Exception _propertyError;
    Exception _typeError;
    Exception _piiError;
    Exception _encryptedError;

    void Because()
    {
        _propertyError = Catch.Exception(() => _generator.Generate(typeof(Outer)));
        _typeError = Catch.Exception(() => _generator.Generate(typeof(EncryptedOuter)));
        _piiError = Catch.Exception(() => _generator.Generate(typeof(PersonalOuter)));
        _encryptedError = Catch.Exception(() => _generator.Generate(typeof(SecretOuter)));
    }

    [Fact] void should_reject_a_conflicting_property_scope_pushed_to_a_nested_concept() => _propertyError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_a_conflicting_type_scope_pushed_to_a_nested_concept() => _typeError.ShouldBeOfExactType<ConflictingEncryptionScopes>();
    [Fact] void should_reject_pii_pushed_to_an_encrypted_nested_concept() => _piiError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
    [Fact] void should_reject_encryption_pushed_to_a_pii_nested_concept() => _encryptedError.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
}
