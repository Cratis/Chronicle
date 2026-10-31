// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_skip_range_is_empty_or_inverted : given.a_valid_commit
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    void should_reject_non_increasing_ranges(ulong to)
    {
        var commit = Create(_root, [], [SkipRangeOperation.Insert(_skip with { From = new(1), To = new(to) })]);
        Catch.Exception(() => DeliveryCommitValidator.Validate(commit)).ShouldBeOfExactType<InvalidDeliveryCommit>();
    }
}
