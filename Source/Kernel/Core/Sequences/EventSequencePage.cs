// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// A bounded page in read order, with a continuation only when more matching events exist.
/// </summary>
/// <param name="Events">The events in read order.</param>
/// <param name="Next">The next inclusive start, or null at the end of the matching range.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record EventSequencePage(IEnumerable<AppendedEvent> Events, ulong? Next)
{
    /// <summary>
    /// Reads a bounded keyset page in read order, with inclusive start and stop bounds.
    /// </summary>
    /// <param name="storage">The storage provider.</param>
    /// <param name="eventCompliance">The event compliance manager.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <param name="from">Inclusive start; null starts at the beginning or tail in the read direction.</param>
    /// <param name="count">Page size, between 1 and 1000.</param>
    /// <param name="backward">Whether to read newest first.</param>
    /// <param name="until">Inclusive stop in the read direction; null means unbounded.</param>
    /// <param name="eventSourceId">Optional event source identifier.</param>
    /// <param name="eventSourceType">Optional event source type.</param>
    /// <param name="eventStreamType">Optional event stream type.</param>
    /// <param name="eventStreamId">Optional event stream identifier.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers.</param>
    /// <param name="tags">Optional comma separated tags to match any of.</param>
    /// <returns>The page with a continuation only when another matching event exists.</returns>
    /// <exception cref="InvalidEventSequencePage">The count is outside 1..1000 or a supplied bound is a special sequence number.</exception>
    public static Task<EventSequencePage> Page(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        ulong? from,
        int count,
        bool backward = false,
        ulong? until = default,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? eventStreamId = default,
        string? eventTypeIds = default,
        string? tags = default) =>
        EventSequenceQuerying.ReadWindow(
            storage,
            eventCompliance,
            jsonSerializerOptions,
            eventStore,
            @namespace,
            eventSequenceId,
            from,
            count,
            backward,
            until,
            EventSequenceQueryCriteriaFactory.Create(new(eventSourceId, eventSourceType, eventStreamType, null, eventTypeIds, tags)),
            EventSequenceQueryCriteriaFactory.Trimmed(eventStreamId));
}
