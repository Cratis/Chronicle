// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_clearing_recovered_partitions_that_has_failed : given.a_failed_partition
{
    void Establish() => state.PartitionRecovered(partition);

    void Because() => state.ClearRecoveringPartitions();

    [Fact] void should_not_be_in_recovery_state() => state.IsRecoveringAnyPartition.ShouldBeFalse();
}
