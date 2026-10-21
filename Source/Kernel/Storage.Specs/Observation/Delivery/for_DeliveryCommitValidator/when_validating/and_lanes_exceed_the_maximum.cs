// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_lanes_exceed_the_maximum : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(WithLanes(_root, [.. Enumerable.Repeat(_root.Lanes[0], 65)]), [], []);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_more_than_sixty_four_lanes() => _exception.ShouldBeOfExactType<DeliveryValueOutOfRange>();
}
