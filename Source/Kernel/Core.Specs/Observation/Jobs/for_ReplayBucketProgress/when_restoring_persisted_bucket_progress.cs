// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_restoring_persisted_bucket_progress : Specification
{
    JobStateWithLastHandledEvent _original;
    JobStateWithLastHandledEvent _restored;

    void Establish() => _original = new()
    {
        ReplayBucketSteps =
        [
            new(Guid.NewGuid(), new(0, 3, 1), 15UL),
            new(Guid.NewGuid(), new(1, 3, 1), 80UL, true),
            new(Guid.NewGuid(), new(2, 3, 1), EventSequenceNumber.Unavailable, true)
        ]
    };

    void Because() => _restored = ReplaySerialization.Restore(_original);

    [Fact] void should_preserve_the_pinned_assignments_and_step_ids() => _restored.ReplayBucketSteps.ShouldEqual(_original.ReplayBucketSteps);
    [Fact] void should_preserve_the_minimum_unfinished_position() => ReplayBucketProgress.Position(_restored.ReplayBucketSteps).ShouldEqual((EventSequenceNumber)15UL);
    [Fact] void should_not_create_per_partition_progress() => _restored.ReplayPartitionSteps.ShouldBeEmpty();
}
