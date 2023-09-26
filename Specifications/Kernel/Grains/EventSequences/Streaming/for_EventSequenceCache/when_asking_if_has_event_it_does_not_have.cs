// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_asking_if_has_event_it_does_not_have : given.an_event_sequence_cache
{
    bool result;

    void Establish() => Enumerable.Range(0, 100).ForEach(_ => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));

    void Because() => result = cache.HasEvent(100);

    [Fact] void should_not_have_event() => result.ShouldBeFalse();
}
