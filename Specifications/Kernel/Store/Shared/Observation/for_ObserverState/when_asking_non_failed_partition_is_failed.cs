// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_asking_non_failed_partition_is_failed : given.a_failed_partition
{
    bool result;

    void Because() => result = state.IsPartitionFailed(Guid.NewGuid());

    [Fact] void should_not_be_failed() => result.ShouldBeFalse();
}
