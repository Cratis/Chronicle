// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_getting_failed_partition : given.a_failed_partition
{
    FailedObserverPartition result;

    void Because() => result = state.GetFailedPartition(partition);

    [Fact] void should_return_partition() => result.EventSourceId.ShouldEqual(partition);
}
