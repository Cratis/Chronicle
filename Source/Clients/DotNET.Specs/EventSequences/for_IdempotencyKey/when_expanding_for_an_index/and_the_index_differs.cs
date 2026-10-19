// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_expanding_for_an_index;

public class and_the_index_differs : Specification
{
    IdempotencyKey _key;

    void Establish() => _key = new("order-1");

    [Fact] void should_derive_distinct_keys() => _key.ForIndex(0).ShouldNotEqual(_key.ForIndex(1));
    [Fact] void should_derive_the_same_key_for_the_same_index() => _key.ForIndex(3).ShouldEqual(_key.ForIndex(3));
}
