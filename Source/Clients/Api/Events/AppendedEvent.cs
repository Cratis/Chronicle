// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.EventSequences;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Context">The context for the event.</param>
/// <param name="Content">The JSON representation content of the event.</param>
/// <param name="OriginalContent">The original JSON content before any revisions. Only present when revised.</param>
/// <param name="Revisions">The revisions applied to this event, if any.</param>
/// <param name="GenerationalContent">Content for each generation stored for this event, keyed by generation number.</param>
[ReadModel]
public record AppendedEvent(
    EventContext Context,
    string Content,
    string OriginalContent = "",
    IEnumerable<EventRevision>? Revisions = null,
    IEnumerable<KeyValuePair<int, string>>? GenerationalContent = null)
{
    /// <summary>
    /// Query events in an event sequence, narrowed and ordered by the values a saved query carries.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> for working with event sequences.</param>
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
        IEventSequences eventSequences,
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

        var response = await eventSequences.QueryEvents(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Criteria = EventSequenceQueryCriteriaFactory.Create(new(
                eventSourceId,
                eventSourceType,
                eventStreamType,
                correlationId,
                eventTypeIds,
                tags,
                occurredFrom,
                occurredTo)),
            Skip = paging.IsPaged ? paging.Page * paging.Size : 0,
            Take = paging.IsPaged ? paging.Size : int.MaxValue,
            Descending = descending,
            SortBy = sortBy
        });

        // Paging is over the events matching the criteria, not over the whole sequence - the tail
        // sequence number would overcount as soon as any filter is set.
        queryContext.TotalItems = (int)response.TotalCount;

        return response.Events.ToApi();
    }
}
