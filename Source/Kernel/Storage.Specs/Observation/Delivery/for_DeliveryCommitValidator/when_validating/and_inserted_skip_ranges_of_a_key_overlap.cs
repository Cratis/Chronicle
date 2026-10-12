// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_inserted_skip_ranges_of_a_key_overlap : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root, [], [SkipRangeOperation.Insert(_skip with { From = new(1), To = new(3) }), SkipRangeOperation.Insert(_skip with { To = new(2) })]);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_overlapping_ranges() => _exception.ShouldBeOfExactType<InvalidDeliveryCommit>();
}
