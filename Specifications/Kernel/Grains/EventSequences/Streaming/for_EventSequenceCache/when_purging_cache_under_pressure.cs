// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_purging_cache_under_pressure : given.an_event_sequence_cache
{
    void Establish() => Enumerable.Range(0, EventSequenceCache.MaxNumberOfEvents + 100).ForEach(_ => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));

    void Because() => cache.Purge();

    [Fact] void should_not_be_under_pressure_anymore() => cache.IsUnderPressure().ShouldBeFalse();
    [Fact] void should_only_have_max_number_of_events() => cache.Count.ShouldEqual(EventSequenceCache.MaxNumberOfEvents - EventSequenceCache.NumberOfEventsToPurge);
}
