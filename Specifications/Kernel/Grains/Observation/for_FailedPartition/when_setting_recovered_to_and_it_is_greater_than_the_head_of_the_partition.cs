// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_setting_recovered_to_and_it_is_greater_than_the_head_of_the_partition : Specification
{
    FailedPartition failed_partition;

    void Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, Enumerable.Empty<string>(), string.Empty, DateTimeOffset.UtcNow)
        {
            Head = 4
        };
    }

    Task Because() => Task.FromResult(failed_partition.SetRecoveredTo(5));

    [Fact] void should_be_recovered() => failed_partition.IsRecovered.ShouldBeTrue();
}
