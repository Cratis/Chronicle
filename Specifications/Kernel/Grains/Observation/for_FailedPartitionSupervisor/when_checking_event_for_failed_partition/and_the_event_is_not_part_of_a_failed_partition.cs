// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.given;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.when_checking_event_for_failed_partition;

public class and_the_event_is_not_part_of_a_failed_partition : a_supervisor
{
    FailedPartitionSupervisor supervisor;
    bool is_for_failed_partition;

    Task Establish()
    {
        supervisor = get_clean_supervisor();
        return Task.CompletedTask;
    }
    
    Task Because()
    {
        is_for_failed_partition = supervisor.EventBelongsToFailingPartition(Guid.NewGuid(), EventSequenceNumber.First, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
    
    [Fact] void should_not_be_for_failed_partition() => is_for_failed_partition.ShouldBeFalse();
}