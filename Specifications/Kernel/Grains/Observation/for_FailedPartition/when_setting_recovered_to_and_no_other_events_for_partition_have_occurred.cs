// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_setting_recovered_to_and_no_other_events_for_partition_have_occurred : Specification
{
    FailedPartition failed_partition;
    
    Task Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        return Task.FromResult(failed_partition);
    }

    Task Because() => Task.FromResult(failed_partition.SetRecoveredTo(2));

    [Fact] void should_be_recovered() => failed_partition.IsRecovered.ShouldBeTrue();
}