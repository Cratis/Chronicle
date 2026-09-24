// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueKeyIdentifiers;

public class when_building_for_namespace : Specification
{
    EncryptionKeyIdentifier _result;

    void Because() => _result = EncryptedValueKeyIdentifiers.ForNamespace();

    [Fact] void should_be_recognized_as_an_encrypted_value_identifier() => EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(_result).ShouldBeTrue();
    [Fact] void should_be_deterministic() => EncryptedValueKeyIdentifiers.ForNamespace().ShouldEqual(_result);
    [Fact] void should_not_equal_a_subject_scoped_identifier() => _result.ShouldNotEqual(EncryptedValueKeyIdentifiers.ForSubject("some-subject"));
    [Fact] void should_not_equal_the_global_identifier() => _result.ShouldNotEqual(EncryptedValueKeyIdentifiers.ForGlobal());
}
