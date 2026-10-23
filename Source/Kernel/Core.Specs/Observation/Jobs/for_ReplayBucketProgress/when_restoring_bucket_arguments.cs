// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_restoring_bucket_arguments : Specification
{
    HandleEventsForObserverArguments _restored;

    void Because() => _restored = ReplaySerialization.Restore(new HandleEventsForObserverArguments(
        ObserverKey.NotSet, ObserverType.Projection, EventSequenceNumber.First, EventSequenceNumber.Max, EventObservationState.Replay, [])
    {
        Bucket = new(3, 8, 1),
        Revision = 42
    });

    [Fact] void should_keep_the_original_bucket_index_count_and_hash_version() => _restored.Bucket.ShouldEqual(new ReplayBucket(3, 8, 1));
    [Fact] void should_keep_the_replay_revision() => _restored.Revision.ShouldEqual(42UL);
}
