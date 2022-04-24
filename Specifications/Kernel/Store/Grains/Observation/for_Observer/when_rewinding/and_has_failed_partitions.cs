// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_rewinding;

public class and_has_failed_partitions : given.an_observer_and_two_event_types
{
    void Establish()
    {
        state.FailPartition("b3cc41d8-2354-4444-9427-c8520d89ae8d", 42, Array.Empty<string>(), string.Empty);
        state.FailPartition("90c9c6ef-a1b2-479f-ad0b-ad9893356341", 43, Array.Empty<string>(), string.Empty);
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_clear_failed_partitions() => state.HasFailedPartitions.ShouldBeFalse();
}
