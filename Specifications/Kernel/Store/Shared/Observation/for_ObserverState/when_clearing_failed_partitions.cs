// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_clearing_failed_partitions : given.a_failed_partition
{
    void Because() => state.ClearFailedPartitions();

    [Fact] void should_not_have_any_failed_partitions() => state.HasFailedPartitions.ShouldBeFalse();
}
