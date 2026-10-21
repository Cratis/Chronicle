// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_expanding_for_an_index;

public class and_the_key_is_512_characters : Specification
{
    IdempotencyKey _derived;

    void Because() => _derived = new IdempotencyKey(new string('a', 512)).ForIndex(7);

    [Fact] void should_derive_a_key_of_64_characters() => _derived.Value.Length.ShouldEqual(64);
}
