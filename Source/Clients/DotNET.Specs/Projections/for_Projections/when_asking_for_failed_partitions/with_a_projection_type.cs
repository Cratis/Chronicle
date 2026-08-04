// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_Projections.when_asking_for_failed_partitions;

public class with_a_projection_type : given.a_discovered_projection
{
    FailedPartition _failedPartition;
    IEnumerable<FailedPartition> _result;

    void Establish()
    {
        _failedPartition = new FailedPartition(Guid.NewGuid(), "the-projection", "the-partition", []);
        _failedPartitions.GetFailedPartitionsFor(Arg.Any<ObserverId>()).Returns([_failedPartition]);
    }

    async Task Because() => _result = await _projections.GetFailedPartitionsFor<TheProjection>();

    [Fact] void should_ask_for_the_failed_partitions_of_the_projection() => _failedPartitions.Received(1).GetFailedPartitionsFor(_projections.GetProjectionIdFor<TheProjection>().Value);
    [Fact] void should_return_the_failed_partitions() => _result.ShouldContainOnly(_failedPartition);
}
