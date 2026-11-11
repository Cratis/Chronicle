// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_live_delivery_follows_a_replay_failure : given.an_observer
{
    readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000005827"));

    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["Replay failure"], "Stack", FailureKind.Handling, null, null, new PartitionFailureContext(ReplayEpoch: _epoch));

    async Task Because() => await _observer.PartitionFailed("partition", 43UL, ["Live failure"], "Stack");

    [Fact] void should_preserve_the_replay_epoch() => _failedPartitionsState.Partitions.Single().ReplayEpoch.ShouldEqual(_epoch);
    [Fact] void should_record_both_attempts() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(2);
}
