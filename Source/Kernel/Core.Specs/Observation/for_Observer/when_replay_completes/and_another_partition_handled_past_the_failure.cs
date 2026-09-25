// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_another_partition_handled_past_the_failure : given.an_observer
{
    static readonly Key _failedPartition = "failed-partition";
    static readonly Key _otherPartition = "other-partition";

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_failedPartition, 12UL);
    }

    async Task Because() => await _observer.ReplayedSuccessfully(42UL, new Dictionary<Key, EventSequenceNumber> { [_otherPartition] = 42UL }, []);

    [Fact] void should_keep_failed_partition() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_keep_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
