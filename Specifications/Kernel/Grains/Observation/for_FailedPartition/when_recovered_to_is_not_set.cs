// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartition;

public class when_recovered_to_is_not_set : Specification
{
    FailedPartition failed_partition;

    Task Establish()
    {
        failed_partition = new FailedPartition(Guid.NewGuid(), 2, Enumerable.Empty<string>(), string.Empty, DateTimeOffset.UtcNow)
        {
            Head = 4
        };
        return Task.FromResult(failed_partition);
    }

    [Fact] void should_not_be_recovered() => failed_partition.IsRecovered.ShouldBeFalse();
}
