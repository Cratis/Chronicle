// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_priming_the_cache : given.an_event_sequence_cache
{
    EventSequenceNumber from;
    Mock<IEventCursor> event_cursor;
    AppendedEvent[] fetched_events;

    void Establish()
    {
        from = (ulong)(Random.Shared.Next() % 1000);
        event_cursor = new();
        event_cursor.SetupSequence(_ => _.MoveNext())
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        event_sequence_storage.Setup(_ =>
            _.GetRange(from, from + EventSequenceCache.NumberOfEventsToFetch, null, null, default))
            .Returns(Task.FromResult(event_cursor.Object));

        fetched_events = Enumerable
            .Range((int)from.Value, (int)(from + EventSequenceCache.NumberOfEventsToFetch).Value)
            .Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray();
        event_cursor.SetupGet(_ => _.Current)
            .Returns(fetched_events);
    }

    void Because() => cache.Prime(from);

    [Fact] void should_hold_all_the_events_fetched() => cache.Events.ShouldContainOnly(fetched_events);
}
