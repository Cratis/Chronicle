// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_adding_existing_event : given.an_event_sequence_cache
{
    void Establish()
    {
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(3));
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(4));
    }

    void Because() => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(4));

    [Fact] void should_not_add_event() => cache.Count.ShouldEqual(2);
}
