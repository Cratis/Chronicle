// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_asking_whether_internal_secrets_are_kept_outside_the_databases : Specification
{
    CompositeEncryptionKeyStorage _outsidePrimary;
    CompositeEncryptionKeyStorage _insidePrimary;
    InMemoryEncryptionKeyStorage _byDefault;

    void Establish()
    {
        var outside = Substitute.For<IEncryptionKeyStorage>();
        outside.KeepsInternalSecretsOutsideTheEventStoreDatabases.Returns(true);
        var inside = Substitute.For<IEncryptionKeyStorage>();
        inside.KeepsInternalSecretsOutsideTheEventStoreDatabases.Returns(false);

        // Internal secrets live only in the primary, so the secondary's answer does not count.
        _outsidePrimary = new(outside, inside);
        _insidePrimary = new(inside, outside);
        _byDefault = new();
    }

    [Fact] void should_say_what_the_primary_says_when_it_is_outside() => _outsidePrimary.KeepsInternalSecretsOutsideTheEventStoreDatabases.ShouldBeTrue();
    [Fact] void should_say_what_the_primary_says_when_it_is_not() => _insidePrimary.KeepsInternalSecretsOutsideTheEventStoreDatabases.ShouldBeFalse();
    [Fact] void should_not_claim_it_for_a_store_that_has_not_said_so() => ((IEncryptionKeyStorage)_byDefault).KeepsInternalSecretsOutsideTheEventStoreDatabases.ShouldBeFalse();
}
