// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_creating;

public class with_513_characters : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new IdempotencyKey(new string('a', 513)));

    [Fact] void should_refuse_the_key() => _error.ShouldBeOfExactType<IdempotencyKeyNotValid>();
}
