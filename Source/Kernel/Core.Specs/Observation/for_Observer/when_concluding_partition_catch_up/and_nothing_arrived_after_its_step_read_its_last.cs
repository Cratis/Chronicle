// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_concluding_partition_catch_up;

/// <summary>
/// Nothing was appended for the partition after its step read its last event, so it is handed back to live delivery
/// right away rather than held back until the whole catch-up job has reported back.
/// </summary>
public class and_nothing_arrived_after_its_step_read_its_last : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partitionA = "partition-a";
    static readonly Key _partitionB = "partition-b";

    bool _concluded;

    void Establish()
    {
        _stateStorage.State.CatchingUpPartitions.Add(_partitionA);
        _stateStorage.State.CatchingUpPartitions.Add(_partitionB);
        _storageStats.ResetCounts();
    }

    async Task Because() => _concluded = await _observer.ConcludePartitionCatchUp(_partitionB, 10UL, [event_type]);

    [Fact] void should_conclude_the_partition() => _concluded.ShouldBeTrue();
    [Fact] void should_hand_the_partition_back_to_live_delivery() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partitionB);
    [Fact] void should_leave_the_other_partition_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partitionA);
    [Fact] void should_persist_the_release() => _storageStats.Writes.ShouldEqual(1);
}
