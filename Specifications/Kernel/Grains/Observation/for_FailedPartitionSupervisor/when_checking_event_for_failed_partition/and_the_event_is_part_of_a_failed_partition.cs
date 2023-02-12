// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.given;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.when_checking_event_for_failed_partition;

public class and_the_event_is_part_of_a_failed_partition : a_supervisor
{
    EventSourceId partition_id;
    FailedPartitionSupervisor supervisor;
    bool is_for_failed_partition;

    Task Establish()
    {
        partition_id = Guid.NewGuid();
        supervisor = get_supervisor_with_failed_partition(partition_id);
        return Task.CompletedTask;
    }

    Task Because()
    {
        is_for_failed_partition = supervisor.EventBelongsToFailingPartition(partition_id, 3);
        return Task.CompletedTask;
    }

    [Fact] void should_be_for_failed_partition() => is_for_failed_partition.ShouldBeTrue();
    [Fact] void should_increase_the_head_of_the_failed_partition() => supervisor.GetState().FailedPartitions.Single(_ => _.Partition == partition_id).Head.ShouldEqual(new EventSequenceNumber(3));
}