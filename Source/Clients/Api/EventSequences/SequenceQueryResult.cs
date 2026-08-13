// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents an event returned by a workbench event sequence query.
/// </summary>
/// <remarks>
/// This is the paged and filtered counterpart to the plain appended-events read: the narrowing and the
/// ordering happen in storage, so a query over a large sequence only transfers the page being shown.
/// </remarks>
[ReadModel]
public record SequenceQueryResult
{
    /// <summary>
    /// Query events in an event sequence, narrowed and ordered by the values a saved query carries.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> for working with event sequences.</param>
    /// <param name="queryContextManager"><see cref="IQueryContextManager"/> for reading the requested page.</param>
    /// <param name="eventStore">Event store to query.</param>
    /// <param name="namespace">Namespace to query.</param>
    /// <param name="eventSequenceId">Event sequence to query.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <param name="descending">Whether to order from the newest event down rather than from the oldest up.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    public static async Task<IEnumerable<AppendedEvent>> QueryEvents(
        IEventSequences eventSequences,
        IQueryContextManager queryContextManager,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string? eventSourceId = default,
        string? eventTypeIds = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default,
        bool descending = false)
    {
        var queryContext = queryContextManager.Current;
        var paging = queryContext.Paging;

        var response = await eventSequences.QueryEvents(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Criteria = EventSequenceQueryCriteriaFactory.Create(eventSourceId, eventTypeIds, occurredFrom, occurredTo),
            Skip = paging.IsPaged ? paging.Page * paging.Size : 0,
            Take = paging.IsPaged ? paging.Size : int.MaxValue,
            Descending = descending
        });

        // Paging is over the events matching the criteria, not over the whole sequence - the tail
        // sequence number would overcount as soon as any filter is set.
        queryContext.TotalItems = (int)response.TotalCount;

        return response.Events.ToApi();
    }
}
