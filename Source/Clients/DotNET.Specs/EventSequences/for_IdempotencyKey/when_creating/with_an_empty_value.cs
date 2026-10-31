// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_creating;

public class with_an_empty_value : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new IdempotencyKey(string.Empty));

    [Fact] void should_refuse_the_key() => _error.ShouldBeOfExactType<IdempotencyKeyNotValid>();
}
