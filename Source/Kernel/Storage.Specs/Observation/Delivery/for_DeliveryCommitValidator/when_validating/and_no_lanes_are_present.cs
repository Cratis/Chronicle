// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_no_lanes_are_present : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(WithLanes(_root, []), [], []);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_the_missing_lane() => _exception.ShouldBeOfExactType<DeliveryValueOutOfRange>();
}
