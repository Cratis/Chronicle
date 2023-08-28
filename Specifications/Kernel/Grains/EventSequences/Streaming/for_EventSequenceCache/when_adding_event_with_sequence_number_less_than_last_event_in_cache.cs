// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_adding_event_with_sequence_number_less_than_last_event_in_cache : given.an_event_sequence_cache
{
    Exception exception;

    void Establish()
    {
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(3));
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(4));
    }

    void Because() => exception = Catch.Exception(() => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(1)));

    [Fact] void should_throw_event_sequence_number_is_less_than_last_event_in_cache() => exception.ShouldBeOfExactType<EventSequenceNumberIsLessThanLastEventInCache>();
}
