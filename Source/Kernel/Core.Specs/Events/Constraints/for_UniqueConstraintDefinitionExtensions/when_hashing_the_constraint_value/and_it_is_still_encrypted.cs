// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinitionExtensions.when_hashing_the_constraint_value;

public class and_it_is_still_encrypted : given.a_pii_value_encrypted_per_subject
{
    string _firstCiphertext;
    string _secondCiphertext;

    async Task Because()
    {
        _firstCiphertext = (await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, FirstSubject, JsonValue.Create(Value))).ToString();
        _secondCiphertext = (await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, SecondSubject, JsonValue.Create(Value))).ToString();
    }

    [Fact] void should_encrypt_the_same_value_to_different_ciphertext() => _firstCiphertext.ShouldNotEqual(_secondCiphertext);
    [Fact] void should_hash_the_ciphertext_to_non_colliding_values() => HashOf(_firstCiphertext).ShouldNotEqual(HashOf(_secondCiphertext));
}
