// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.when_resetting;

public class and_there_is_a_failed_partitions : given.a_supervisor
{
    FailedPartitionSupervisor supervisor;
    EventSourceId failed_partition;
    Mock<IRecoverFailedPartition> recover_failed_partition_grain;
    EventSequenceNumber recover_from;

    void Establish()
    {
        failed_partition = EventSourceId.New();
        recover_failed_partition_grain = new();
        recover_from = (ulong)Random.Shared.Next(1000);
        supervisor = get_supervisor_with_failed_partition(failed_partition, recover_from);
        grain_factory.Setup(_ => _.GetGrain<IRecoverFailedPartition>(It.IsAny<Guid>(), It.IsAny<string>(), null)).Returns(recover_failed_partition_grain.Object);
    }

    async Task Because() => await supervisor.Reset();

    [Fact] void should_call_reset_on_recover_grain() => recover_failed_partition_grain.Verify(_ => _.Reset(), Once);
}
