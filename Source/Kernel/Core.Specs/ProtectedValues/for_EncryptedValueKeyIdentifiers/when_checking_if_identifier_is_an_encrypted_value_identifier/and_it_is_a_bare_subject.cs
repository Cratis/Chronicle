// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueKeyIdentifiers.when_checking_if_identifier_is_an_encrypted_value_identifier;

/// <summary>
/// The bare subject value is exactly what PIICompliancePropertyValueHandler passes straight through as its
/// EncryptionKeyIdentifier - unchanged by this feature. It must never be mistaken for an encrypted-value
/// identifier, or PIIManager's defense-in-depth guard would refuse a lawful PII erasure.
/// </summary>
public class and_it_is_a_bare_subject : Specification
{
    static readonly EncryptionKeyIdentifier _bareSubjectIdentifier = "39b34712-ad8e-4cde-b879-2719c995aa49";

    bool _result;

    void Because() => _result = EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(_bareSubjectIdentifier);

    [Fact] void should_not_be_recognized() => _result.ShouldBeFalse();
}
