// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_restoring_persisted_bucket_progress : Specification
{
    List<ReplayBucketStep> _original;
    List<ReplayBucketStep> _restored;

    void Establish() => _original =
    [
        new(Guid.NewGuid(), new(0, 3, 1), 15UL),
        new(Guid.NewGuid(), new(1, 3, 1), 80UL, true),
        new(Guid.NewGuid(), new(2, 3, 1), EventSequenceNumber.Unavailable, true)
    ];

    void Because() => _restored = ReplaySerialization.Restore(_original);

    [Fact] void should_preserve_the_pinned_assignments_and_step_ids() => _restored.SequenceEqual(_original).ShouldBeTrue();
    [Fact] void should_preserve_the_minimum_unfinished_position() => ReplayBucketProgress.Position(_restored).ShouldEqual((EventSequenceNumber)15UL);
}
