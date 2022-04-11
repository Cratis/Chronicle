// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_recovering_failed_partition_is_recovered : given.a_failed_partition
{
    void Establish() => state.StartRecoveringPartition(partition);

    void Because() => state.PartitionRecovered(partition);

    [Fact] void should_not_have_any_failed_partitions() => state.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_not_have_one_partition_being_recovered() => state.RecoveringPartitions.ShouldBeEmpty();
}
