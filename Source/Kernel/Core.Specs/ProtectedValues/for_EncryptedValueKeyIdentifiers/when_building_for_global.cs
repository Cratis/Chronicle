// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueKeyIdentifiers;

public class when_building_for_global : Specification
{
    EncryptionKeyIdentifier _result;

    void Because() => _result = EncryptedValueKeyIdentifiers.ForGlobal();

    [Fact] void should_be_recognized_as_an_encrypted_value_identifier() => EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(_result).ShouldBeTrue();
    [Fact] void should_be_deterministic() => EncryptedValueKeyIdentifiers.ForGlobal().ShouldEqual(_result);
    [Fact] void should_not_equal_a_subject_scoped_identifier() => _result.ShouldNotEqual(EncryptedValueKeyIdentifiers.ForSubject("some-subject"));
    [Fact] void should_not_equal_the_namespace_scoped_identifier() => _result.ShouldNotEqual(EncryptedValueKeyIdentifiers.ForNamespace());
    [Fact] void should_have_a_fixed_global_event_store_distinct_from_not_set() =>
        EncryptedValueKeyIdentifiers.GlobalEventStore.ShouldNotEqual(Concepts.EventStoreName.NotSet);
    [Fact] void should_have_a_fixed_global_namespace_distinct_from_not_set() =>
        EncryptedValueKeyIdentifiers.GlobalNamespace.ShouldNotEqual(Concepts.EventStoreNamespaceName.NotSet);
}
