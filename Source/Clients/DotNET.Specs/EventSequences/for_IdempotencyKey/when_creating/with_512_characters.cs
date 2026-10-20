// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_creating;

public class with_512_characters : Specification
{
    IdempotencyKey _key;

    void Because() => _key = new IdempotencyKey(new string('a', 512));

    [Fact] void should_accept_the_key() => _key.Value.Length.ShouldEqual(512);
}
