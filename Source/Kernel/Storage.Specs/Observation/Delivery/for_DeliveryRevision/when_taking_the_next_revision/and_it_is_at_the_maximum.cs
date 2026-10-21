// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryRevision.when_taking_the_next_revision;

public class and_it_is_at_the_maximum : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new DeliveryRevision(long.MaxValue).Next());

    [Fact] void should_fail_without_wrapping() => _exception.ShouldBeOfExactType<DeliveryRevisionExhausted>();
}
