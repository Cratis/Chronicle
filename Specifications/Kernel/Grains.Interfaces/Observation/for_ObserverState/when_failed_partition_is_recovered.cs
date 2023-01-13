// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverState;

public class when_failed_partition_is_recovered : given.a_failed_partition
{
    void Because() => state.PartitionRecovered(partition);

    [Fact] void should_not_have_any_failed_partitions() => state.FailedPartitions.ShouldBeEmpty();
}
