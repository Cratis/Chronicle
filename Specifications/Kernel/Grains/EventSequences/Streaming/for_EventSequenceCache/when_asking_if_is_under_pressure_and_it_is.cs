// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_asking_if_is_under_pressure_and_it_is : given.an_event_sequence_cache
{
    bool result;

    void Establish() => Enumerable.Range(0, EventSequenceCache.MaxNumberOfEvents + 100).ForEach(_ => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));

    void Because() => result = cache.IsUnderPressure();

    [Fact] void should_be_under_pressure() => result.ShouldBeTrue();
}
