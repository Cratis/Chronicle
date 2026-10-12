// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_row_revision_differs_from_the_root : given.a_valid_commit
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_reject_either_row_namespace(bool parked)
    {
        var commit = parked
            ? Create(_root, [ParkedKeyOperation.Insert(_parked with { Revision = new(2) })], [])
            : Create(_root, [], [SkipRangeOperation.Insert(_skip with { Revision = new(2) })]);
        Catch.Exception(() => DeliveryCommitValidator.Validate(commit)).ShouldBeOfExactType<InvalidDeliveryCommit>();
    }
}
