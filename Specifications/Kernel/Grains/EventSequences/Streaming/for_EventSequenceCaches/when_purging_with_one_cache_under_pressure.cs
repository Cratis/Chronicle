// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCaches;

public class when_purging_with_one_cache_under_pressure : given.two_event_sequence_caches
{
    void Establish()
    {
        first_cache.Setup(_ => _.IsUnderPressure()).Returns(false);
        second_cache.Setup(_ => _.IsUnderPressure()).Returns(true);
    }

    void Because() => caches.Purge();

    [Fact] void should_not_purge_first_cache() => first_cache.Verify(_ => _.Purge(), Never);
    [Fact] void should_purge_second_cache() => second_cache.Verify(_ => _.Purge(), Once);
}
