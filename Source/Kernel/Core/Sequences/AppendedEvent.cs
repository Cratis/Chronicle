// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Id">The identity of the event within its sequence, which is its sequence number.</param>
/// <param name="Context">The context for the event.</param>
/// <param name="Content">The JSON representation content of the event.</param>
/// <param name="OriginalContent">The original JSON content before any revisions. Only present when revised.</param>
/// <param name="Revisions">The revisions applied to this event, if any.</param>
/// <param name="GenerationalContent">Content for each generation stored for this event, keyed by generation number.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendedEvent(
    string Id,
    EventContext Context,
    string Content,
    string OriginalContent = "",
    IEnumerable<EventRevision>? Revisions = null,
    IEnumerable<KeyValuePair<int, string>>? GenerationalContent = null)
{
    /// <summary>
    /// Query events in an event sequence, narrowed and ordered by the values a saved query carries.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="queryContextManager"><see cref="IQueryContextManager"/> for the paging the caller asked for.</param>
    /// <param name="eventStore">Event store to query.</param>
    /// <param name="namespace">Namespace to query.</param>
    /// <param name="eventSequenceId">Event sequence to query.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventSourceType">Optional event source type to narrow to.</param>
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <param name="correlationId">Optional correlation identifier to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="tags">Optional comma separated tags to narrow to - an event matches when it carries any of them.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    /// <remarks>
    /// The narrowing and the ordering happen in storage, so a query over a large sequence only
    /// transfers the page shown. Ordering comes from the sorting Arc resolved onto the query
    /// context, so the caller asks for it the same way it does on any other query.
    /// </remarks>
    public static async Task<IEnumerable<AppendedEvent>> QueryEvents(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        IQueryContextManager queryContextManager,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? correlationId = default,
        string? eventTypeIds = default,
        string? tags = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default)
    {
        var queryContext = queryContextManager.Current;
        var paging = queryContext.Paging;
        var (sortBy, descending) = EventSequenceQuerySortByParser.From(queryContext.Sorting);
        var criteria = EventSequenceQueryCriteriaFactory.Create(new(
            eventSourceId,
            eventSourceType,
            eventStreamType,
            correlationId,
            eventTypeIds,
            tags,
            occurredFrom,
            occurredTo));

        var (events, totalCount) = await EventSequenceQuerying.QueryPage(
            storage,
            eventCompliance,
            eventStore,
            @namespace,
            eventSequenceId,
            criteria,
            paging.IsPaged ? paging.Page * paging.Size : 0,
            paging.IsPaged ? paging.Size : int.MaxValue,
            new EventSequenceQuerySort(sortBy, descending));

        // Paging is over the events matching the criteria, not over the whole sequence - the tail
        // sequence number would overcount as soon as any filter is set.
        queryContext.TotalItems = (int)totalCount;

        return events.ToApi(jsonSerializerOptions);
    }

    /// <summary>
    /// Gets a page of the events in an event sequence.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="queryContextManager">The <see cref="IQueryContextManager"/> carrying paging.</param>
    /// <param name="eventStore">The event store the sequence belongs to.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="eventSequenceId">The event sequence to read.</param>
    /// <param name="eventSourceId">Optional event source to narrow the read to.</param>
    /// <returns>A page of appended events.</returns>
    /// <remarks>
    /// The total is the sequence tail rather than a count of the page, so the caller can page without a second
    /// round trip.
    /// </remarks>
    internal static async Task<IEnumerable<AppendedEvent>> AppendedEvents(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        IQueryContextManager queryContextManager,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string? eventSourceId = default)
    {
        var queryContext = queryContextManager.Current;
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

        var tail = await eventSequence.GetTailSequenceNumber();
        queryContext.TotalItems = (int)tail.Value;

        var paging = queryContext.Paging;
        var from = (ulong)(paging.Page * paging.Size);
        Concepts.Events.EventSourceId? resolvedEventSourceId = null;
        if (!string.IsNullOrWhiteSpace(eventSourceId))
        {
            resolvedEventSourceId = eventSourceId;
        }

        var appendedEvents = new List<Concepts.Events.AppendedEvent>();
        using (var cursor = paging.IsPaged
            ? await eventSequence.GetRange(from, from + (ulong)(paging.Size - 1), resolvedEventSourceId)
            : await eventSequence.GetFromSequenceNumber(from, resolvedEventSourceId))
        {
            while (await cursor.MoveNext())
            {
                appendedEvents.AddRange(cursor.Current);
            }
        }

        var released = await EventSequenceQuerying.ReleaseCompliance(appendedEvents, storage, eventStore, eventCompliance);
        return released.ToApi(jsonSerializerOptions);
    }

    /// <summary>
    /// Gets every event for a specific event source, optionally narrowed to specific event types.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="eventStore">Event store to read from.</param>
    /// <param name="namespace">Namespace to read from.</param>
    /// <param name="eventSequenceId">Event sequence to read from.</param>
    /// <param name="eventSourceId">The event source to get events for.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <param name="eventStreamId">Optional event stream to narrow to.</param>
    /// <returns>Every matching event, unpaged.</returns>
    internal static Task<IEnumerable<AppendedEvent>> ForEventSourceIdAndEventTypes(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string eventSourceId,
        string? eventTypeIds = default,
        string? eventStreamType = default,
        string? eventStreamId = default) =>
        EventSequenceQuerying.ReadFromSequenceNumber(
            storage,
            eventCompliance,
            jsonSerializerOptions,
            eventStore,
            @namespace,
            eventSequenceId,
            Concepts.Events.EventSequenceNumber.First,
            eventSourceId,
            eventTypeIds,
            eventStreamType,
            eventStreamId);

    /// <summary>
    /// Gets every event from a specific sequence number onward, optionally narrowed to an event source and event
    /// types.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="eventStore">Event store to read from.</param>
    /// <param name="namespace">Namespace to read from.</param>
    /// <param name="eventSequenceId">Event sequence to read from.</param>
    /// <param name="fromEventSequenceNumber">The sequence number to start reading from, inclusive.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <returns>Every matching event, unpaged.</returns>
    internal static Task<IEnumerable<AppendedEvent>> FromSequenceNumber(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        ulong fromEventSequenceNumber,
        string? eventSourceId = default,
        string? eventTypeIds = default) =>
        EventSequenceQuerying.ReadFromSequenceNumber(
            storage,
            eventCompliance,
            jsonSerializerOptions,
            eventStore,
            @namespace,
            eventSequenceId,
            fromEventSequenceNumber,
            eventSourceId,
            eventTypeIds);
}
