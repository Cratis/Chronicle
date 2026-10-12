// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_skip_range_identity_has_two_operations : given.a_valid_commit
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_reject_duplicate_identities(bool delete)
    {
        var second = delete ? SkipRangeOperation.Delete(_skip.Generation, _skip.Key.Hash, _skip.From) : SkipRangeOperation.Insert(_skip);
        var commit = Create(_root, [], [SkipRangeOperation.Insert(_skip), second]);
        Catch.Exception(() => DeliveryCommitValidator.Validate(commit)).ShouldBeOfExactType<InvalidDeliveryCommit>();
    }
}
