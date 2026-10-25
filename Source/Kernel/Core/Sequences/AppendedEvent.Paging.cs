// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;

namespace Cratis.Chronicle.Sequences;

public partial record AppendedEvent
{
    /// <summary>
    /// Reads a bounded keyset page. From and until are inclusive in both directions.
    /// Reuse the returned continuation as from when ascending, or until when descending.
    /// </summary>
    /// <param name="storage">The storage provider.</param>
    /// <param name="eventCompliance">The event compliance manager.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="count">Page size, between 1 and 10,000.</param>
    /// <param name="until">Inclusive upper bound; null means unbounded.</param>
    /// <param name="descending">Whether to read newest first.</param>
    /// <param name="eventSourceId">Optional source; empty or unspecified does not narrow.</param>
    /// <param name="eventSourceType">Optional source type; default or unspecified does not narrow.</param>
    /// <param name="eventStreamType">Optional stream type; all or empty does not narrow.</param>
    /// <param name="eventStreamId">Optional stream; default or empty does not narrow.</param>
    /// <param name="eventTypes">Optional event types; null or empty does not narrow.</param>
    /// <param name="tags">Optional tags to match any of; null or empty does not narrow.</param>
    /// <returns>The page with a continuation only when another matching event exists.</returns>
    public static async Task<EventSequencePage> ReadPage(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        Concepts.EventSequences.EventSequenceId eventSequenceId,
        EventSequenceNumber from,
        int count = 100,
        EventSequenceNumber? until = null,
        bool descending = false,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null,
        IEnumerable<Concepts.Events.EventType>? eventTypes = null,
        IEnumerable<Tag>? tags = null)
    {
        ValidatePage(from, until, count);
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var sequence = eventStoreStorage.GetNamespace(@namespace).GetEventSequence(eventSequenceId);
        using var cursor = await sequence.GetWindow(from, until ?? EventSequenceNumber.Max, count + 1, descending,
            eventSourceId, eventSourceType, eventStreamType, eventStreamId, eventTypes, tags);
        var window = new List<KernelAppendedEvent>(count + 1);
        while (window.Count <= count && await cursor.MoveNext())
        {
            window.AddRange(cursor.Current.Take(count + 1 - window.Count));
        }

        var hasMore = window.Count > count;
        var returned = window.Take(count).ToArray();
        var types = returned.Select(_ => _.Context.EventType).Distinct().ToArray();
        var schemas = await eventStoreStorage.EventTypes.GetFor(types);
        var released = await eventCompliance.Release(returned, schemas);
        var events = released.Select(_ => _.ToAppendedEvent(jsonSerializerOptions)).ToArray();
        var next = hasMore ? ContinueAfter(returned[^1].Context.SequenceNumber, descending) : null;
        return new(events, next, hasMore && next is not null);
    }

    static void ValidatePage(EventSequenceNumber from, EventSequenceNumber? until, int count)
    {
        if (count is < 1 or > 10_000)
        {
            throw new InvalidEventSequencePage("The page count must be between 1 and 10,000.");
        }

        if (!from.IsActualValue || from.Value > long.MaxValue ||
            until is not null && (!until.IsActualValue || until.Value > long.MaxValue || from > until))
        {
            throw new InvalidEventSequencePage("Page bounds must be actual signed 64-bit sequence numbers with from no greater than until.");
        }
    }

    static EventSequenceNumber? ContinueAfter(EventSequenceNumber last, bool descending) =>
        descending
            ? last == EventSequenceNumber.First ? null : new(last.Value - 1)
            : last.Value >= long.MaxValue ? null : new(last.Value + 1);
}
