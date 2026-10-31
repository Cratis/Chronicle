// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_position_is_unavailable : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root, [ParkedKeyOperation.Insert(_parked with { Next = EventSequenceNumber.Unavailable })], []);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_the_unavailable_sentinel() => _exception.ShouldBeOfExactType<DeliveryValueOutOfRange>();
}
