// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_asking_if_there_are_failed_partitions_with_none_failed : Specification
{
    bool result;

    void Because() => result = new ObserverState().HasFailedPartitions;

    [Fact] void should_have_failed_partitions() => result.ShouldBeFalse();
}
