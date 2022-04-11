// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_starting_to_recover_failed_partition : given.a_failed_partition
{
    void Because() => state.StartRecoveringPartition(partition);

    [Fact] void should_have_one_partition_being_recovered() => state.RecoveringPartitions.Count().ShouldEqual(1);
}
