// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_getting_event_it_has : given.an_event_sequence_cache
{
    CachedAppendedEvent? result;

    void Establish() => Enumerable.Range(0, 100).ForEach(_ => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));

    void Because() => result = cache.GetEvent(50);

    [Fact] void should_have_event() => result.ShouldNotBeNull();
}
