// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_one_key_records_several_skip_ranges : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root, [], [SkipRangeOperation.Insert(_skip with { From = new(1), To = new(2) }), SkipRangeOperation.Insert(_skip)]);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_accept_adjacent_ranges_regardless_of_input_order() => _exception.ShouldBeNull();
}
