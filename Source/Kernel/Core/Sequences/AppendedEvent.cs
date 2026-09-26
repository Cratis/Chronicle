// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Id">The identity of the event within its sequence, which is its sequence number.</param>
/// <param name="Context">The context for the event.</param>
/// <param name="Content">The JSON representation content of the event.</param>
/// <param name="OriginalContent">The original JSON content before any revisions. Only present when revised.</param>
/// <param name="Revisions">The revisions applied to this event.</param>
/// <param name="GenerationalContent">Content for each generation stored for this event, keyed by generation number.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendedEvent(
    string Id,
    EventContext Context,
    string Content,
    string OriginalContent,
    IEnumerable<EventRevision> Revisions,
    IEnumerable<KeyValuePair<int, string>> GenerationalContent)
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
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        EventSourceId? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? correlationId = default,
        string? eventTypeIds = default,
        string? tags = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default)
    {
        var criteria = EventSequenceQueryCriteriaFactory.Create(new(
            eventSourceId?.Value,
            eventSourceType,
            eventStreamType,
            correlationId,
            eventTypeIds,
            tags,
            occurredFrom,
            occurredTo));

        return await EventSequenceQuerying.QueryEvents(storage, eventCompliance, jsonSerializerOptions, queryContextManager, eventStore, @namespace, eventSequenceId, criteria);
    }

    /// <summary>
    /// Queries a page of events using required structured named tag criteria, alongside the legacy dimensions.
    /// </summary>
    /// <param name="storage">The event storage.</param>
    /// <param name="eventCompliance">The compliance release service.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="queryContextManager">The paging and sorting context.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="namedTags">Structured named tag criteria for gRPC callers; any criterion may match.</param>
    /// <param name="eventSourceId">Optional event source identifier.</param>
    /// <param name="eventSourceType">Optional event source type.</param>
    /// <param name="eventStreamType">Optional event stream type.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers.</param>
    /// <param name="tags">Optional comma separated legacy tags.</param>
    /// <param name="occurredFrom">Optional inclusive occurred bound.</param>
    /// <param name="occurredTo">Optional exclusive occurred bound.</param>
    /// <param name="namedTagsJson">Optional JSON array of named tag criteria for HTTP callers.</param>
    /// <returns>A page of matching appended events.</returns>
    public static Task<IEnumerable<AppendedEvent>> QueryEventsWithNamedTags(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        IQueryContextManager queryContextManager,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        IEnumerable<NamedTagQueryCriterion>? namedTags = default,
        EventSourceId? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? correlationId = default,
        string? eventTypeIds = default,
        string? tags = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default,
        string? namedTagsJson = default)
    {
        var criteria = EventSequenceQueryCriteriaFactory.CreateWithNamedTags(
            new(
                eventSourceId?.Value,
                eventSourceType,
                eventStreamType,
                correlationId,
                eventTypeIds,
                tags,
                occurredFrom,
                occurredTo),
            namedTags,
            namedTagsJson);

        return EventSequenceQuerying.QueryEvents(storage, eventCompliance, jsonSerializerOptions, queryContextManager, eventStore, @namespace, eventSequenceId, criteria);
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
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        EventSourceId? eventSourceId = default)
    {
        var queryContext = queryContextManager.Current;
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

        var tail = await eventSequence.GetTailSequenceNumber();
        queryContext.TotalItems = (int)tail.Value;

        var paging = queryContext.Paging;
        var from = (ulong)(paging.Page * paging.Size);

        // An absent narrowing arrives as an empty value rather than as null, so it has to be treated as "do not narrow".
        var resolvedEventSourceId = string.IsNullOrWhiteSpace(eventSourceId?.Value) ? null : eventSourceId;

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
    /// <param name="eventSourceType">Optional event source type to narrow to.</param>
    /// <returns>Every matching event, unpaged.</returns>
    internal static Task<IEnumerable<AppendedEvent>> ForEventSourceIdAndEventTypes(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string eventSourceId,
        string? eventTypeIds = default,
        string? eventStreamType = default,
        string? eventStreamId = default,
        string? eventSourceType = default) =>
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
            eventStreamId,
            eventSourceType);

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
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        ulong fromEventSequenceNumber,
        EventSourceId? eventSourceId = default,
        string? eventTypeIds = default) =>
        EventSequenceQuerying.ReadFromSequenceNumber(
            storage,
            eventCompliance,
            jsonSerializerOptions,
            eventStore,
            @namespace,
            eventSequenceId,
            fromEventSequenceNumber,
            eventSourceId?.Value,
            eventTypeIds);
}
