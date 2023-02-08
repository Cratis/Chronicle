// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_adding_events_that_is_not_in_sequence : given.an_event_sequence_cache
{
    void Establish()
    {
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(3));
        cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(4));
        var cursor = new Mock<IEventCursor>();
        cursor.SetupSequence(_ => _.MoveNext())
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        cursor.SetupGet(_ => _.Current).Returns(new[] {
            AppendedEvent.EmptyWithEventSequenceNumber(5),
        }.AsEnumerable());

        event_sequence_storage_provider.Setup(_ => _.GetRange(event_sequence_id, 5, 5 + EventSequenceCache.NumberOfEventsToFetch, null, null)).Returns(Task.FromResult(cursor.Object));
    }

    void Because() => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber(6));

    [Fact] void should_prime_cache() => event_sequence_storage_provider.Verify(_ => _.GetRange(event_sequence_id, 5, 5 + EventSequenceCache.NumberOfEventsToFetch, null, null), Once);
    [Fact] void should_add_the_event_after_primed_cache() => cache.EventsBySequenceNumber.ContainsKey(5).ShouldBeTrue();
}
