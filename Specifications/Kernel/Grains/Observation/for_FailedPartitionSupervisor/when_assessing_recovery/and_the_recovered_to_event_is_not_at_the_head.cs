// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.given;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.when_assessing_recovery;

public class and_the_recovered_to_event_is_not_at_the_head : a_supervisor
{
    EventSourceId partition_id;
    FailedPartitionSupervisor supervisor;
    Mock<IRecoverFailedPartition> failed_partition_mock;

    Task Establish()
    {
        partition_id = Guid.NewGuid();
        var partition_key = get_partitioned_observer_key(partition_id);
        failed_partition_mock = a_failed_partition_mock(partition_key.EventSourceId);

        grain_factory.Setup(
            _ => _.GetGrain<IRecoverFailedPartition>(
                observer_id,
                partition_key,
                null)).Returns(failed_partition_mock.Object);
        supervisor = get_supervisor_with_failed_partition(partition_id);
        supervisor.EventBelongsToFailingPartition(partition_id, 3);
        return Task.CompletedTask;
    }

    Task Because() => supervisor.AssessRecovery(partition_id, 2);

    [Fact] void should_still_have_the_failed_partition() => supervisor.GetState().FailedPartitions.Any(_ => _.Partition == partition_id).ShouldBeTrue();
    [Fact] void should_not_have_reset_the_failed_partition_job() => failed_partition_mock.Verify(_ => _.Reset(), Never);
    [Fact] void should_restart_the_failed_partition_job() => failed_partition_mock.Verify(_ => _.Catchup(3), Never);
}
