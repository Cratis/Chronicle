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
/// Represents one event as it appears in an exported file.
/// </summary>
/// <param name="SequenceNumber">The event's position in the sequence.</param>
/// <param name="EventType">The identifier of the event's type.</param>
/// <param name="EventSourceType">The event source type the event belongs to.</param>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventStreamType">The event stream type the event belongs to.</param>
/// <param name="CorrelationId">The correlation the event was appended under.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="Tags">The tags the event carries.</param>
/// <param name="Content">The event's content, as the JSON it is stored as.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record ExportedEvent(
    ulong SequenceNumber,
    string EventType,
    string EventSourceType,
    string EventSourceId,
    string EventStreamType,
    string CorrelationId,
    DateTimeOffset Occurred,
    IEnumerable<string> Tags,
    string Content)
{
    /// <summary>
    /// Get every event matching a set of criteria, for exporting them.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="queryContextManager"><see cref="IQueryContextManager"/> for the ordering the caller asked for.</param>
    /// <param name="eventStore">Event store to export from.</param>
    /// <param name="namespace">Namespace to export from.</param>
    /// <param name="eventSequenceId">Event sequence to export from.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventSourceType">Optional event source type to narrow to.</param>
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <param name="correlationId">Optional correlation identifier to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="tags">Optional comma separated tags to narrow to - an event matches when it carries any of them.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <returns>A collection of <see cref="ExportedEvent"/>.</returns>
    /// <remarks>
    /// Deliberately unpaged: an export covers everything the criteria matches, not the page the
    /// caller happens to be looking at, which is why the whole set is assembled here rather than
    /// walked page by page from the browser.
    /// </remarks>
    public static async Task<IEnumerable<ExportedEvent>> ExportEvents(
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
        var (sortBy, descending) = EventSequenceQuerySortByParser.From(queryContextManager.Current.Sorting);
        var criteria = EventSequenceQueryCriteriaFactory.Create(new(
            eventSourceId,
            eventSourceType,
            eventStreamType,
            correlationId,
            eventTypeIds,
            tags,
            occurredFrom,
            occurredTo));

        var (events, _) = await EventSequenceQuerying.QueryPage(
            storage,
            eventCompliance,
            eventStore,
            @namespace,
            eventSequenceId,
            criteria,
            0,
            int.MaxValue,
            new EventSequenceQuerySort(sortBy, descending));

        // Projected inline rather than through a helper, because every static method on a read model
        // is published as a query of its own.
        return events.ToApi(jsonSerializerOptions).Select(@event => new ExportedEvent(
            @event.Context.SequenceNumber,
            @event.Context.EventType.Id,
            @event.Context.EventSourceType,
            @event.Context.EventSourceId,
            @event.Context.EventStreamType,
            @event.Context.CorrelationId.ToString(),
            @event.Context.Occurred,
            @event.Context.Tags,
            @event.Content)).ToArray();
    }
}
