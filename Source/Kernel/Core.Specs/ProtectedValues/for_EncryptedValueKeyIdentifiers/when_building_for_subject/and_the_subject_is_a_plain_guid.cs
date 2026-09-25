// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueKeyIdentifiers.when_building_for_subject;

public class and_the_subject_is_a_plain_guid : Specification
{
    const string Subject = "39b34712-ad8e-4cde-b879-2719c995aa49";

    EncryptionKeyIdentifier _result;

    void Because() => _result = EncryptedValueKeyIdentifiers.ForSubject(Subject);

    [Fact] void should_not_equal_the_bare_subject_pii_would_use() => ((string)_result).ShouldNotEqual(Subject);
    [Fact] void should_be_recognized_as_an_encrypted_value_identifier() => EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(_result).ShouldBeTrue();
    [Fact] void should_be_deterministic_for_the_same_subject() => EncryptedValueKeyIdentifiers.ForSubject(Subject).ShouldEqual(_result);
}
