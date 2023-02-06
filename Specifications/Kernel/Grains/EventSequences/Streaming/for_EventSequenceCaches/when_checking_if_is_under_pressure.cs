// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCaches;

public class when_checking_if_is_under_pressure : given.two_event_sequence_caches
{
    void Because() => caches.IsUnderPressure();

    [Fact] void should_check_if_first_cache_is_under_pressure() => first_cache.Verify(_ => _.IsUnderPressure(), Once);
    [Fact] void should_check_if_second_cache_is_under_pressure() => second_cache.Verify(_ => _.IsUnderPressure(), Once);
}
