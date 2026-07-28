// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinitionExtensions.when_hashing_the_constraint_value;

public class and_it_has_been_decrypted : given.a_pii_value_encrypted_per_subject
{
    string _firstPlaintext;
    string _secondPlaintext;

    async Task Because()
    {
        var firstCiphertext = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, FirstSubject, JsonValue.Create(Value));
        var secondCiphertext = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, SecondSubject, JsonValue.Create(Value));

        _firstPlaintext = (await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, FirstSubject, firstCiphertext)).ToString();
        _secondPlaintext = (await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, SecondSubject, secondCiphertext)).ToString();
    }

    [Fact] void should_release_the_first_subject_to_the_original_value() => _firstPlaintext.ShouldEqual(Value);
    [Fact] void should_release_the_second_subject_to_the_original_value() => _secondPlaintext.ShouldEqual(Value);
    [Fact] void should_hash_the_decrypted_value_to_colliding_values() => HashOf(_firstPlaintext).ShouldEqual(HashOf(_secondPlaintext));
}
