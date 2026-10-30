// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.EventSequences.for_BoundedEventCursor.given;

public static class events
{
    public static AppendedEvent[] Numbered(ulong from, ulong count) =>
        [.. Enumerable.Range(0, (int)count).Select(offset => For(from + (ulong)offset))];

    public static async Task<List<ulong[]>> Walk(IEventCursor cursor)
    {
        var batches = new List<ulong[]>();
        while (await cursor.MoveNext())
        {
            batches.Add([.. cursor.Current.Select(_ => _.Context.SequenceNumber.Value)]);
        }

        return batches;
    }

    static AppendedEvent For(ulong sequenceNumber) =>
        new(
            new EventContext(
                new EventType("TheEventType", EventTypeGeneration.First),
                EventSourceType.Default,
                "the-source",
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                DateTimeOffset.UtcNow,
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                CorrelationId.NotSet,
                [],
                Identity.System,
                [],
                EventHash.NotSet,
                EventObservationState.None,
                Subject.NotSet),
            new System.Dynamic.ExpandoObject());
}
