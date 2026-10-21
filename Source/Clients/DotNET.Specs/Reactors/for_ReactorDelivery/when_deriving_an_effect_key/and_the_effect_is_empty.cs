// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_deriving_an_effect_key;

public class and_the_effect_is_empty : given.a_delivery
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _delivery.IdempotencyKeyFor(string.Empty));

    [Fact] void should_refuse_it() => _result.ShouldBeOfExactType<ArgumentException>();
}
