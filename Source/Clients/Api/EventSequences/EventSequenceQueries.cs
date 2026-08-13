// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequenceQueries : ControllerBase
{
    readonly IEventSequences _eventSequences;
    readonly IQueryContextManager _queryContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueries"/> class.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> service for working with the event log.</param>
    /// <param name="queryContextManager"><see cref="IQueryContextManager"/> for managing query contexts.</param>
    internal EventSequenceQueries(
        IEventSequences eventSequences,
        IQueryContextManager queryContextManager)
    {
        _eventSequences = eventSequences;
        _queryContextManager = queryContextManager;
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="eventSourceId">Optional event source id to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<AppendedEvent>> AppendedEvents(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] string? eventSourceId = default)
    {
        var queryContext = _queryContextManager.Current;

        var tail = await _eventSequences.GetTailSequenceNumber(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId
        });
        queryContext.TotalItems = (int)tail.SequenceNumber;

        var from = (ulong)(queryContext.Paging.Page * queryContext.Paging.Size);
        var response = await _eventSequences.GetEventsFromEventSequenceNumber(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            FromEventSequenceNumber = from,
            ToEventSequenceNumber = queryContext.Paging.IsPaged ? from + (ulong)(queryContext.Paging.Size - 1) : null,
            EventSourceId = eventSourceId
        });

        return response.Events.ToApi();
    }

    /// <summary>
    /// Query events in an event sequence, narrowed and ordered by the values a saved query carries.
    /// </summary>
    /// <param name="eventStore">Event store to query.</param>
    /// <param name="namespace">Namespace to query.</param>
    /// <param name="eventSequenceId">Event sequence to query.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <param name="descending">Whether to order from the newest event down rather than from the oldest up.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    /// <remarks>
    /// This is the paged and filtered counterpart to <see cref="AppendedEvents"/>: the narrowing and
    /// the ordering happen in storage, so a query over a large sequence only transfers the page shown.
    /// </remarks>
    [HttpGet("query")]
    public async Task<IEnumerable<AppendedEvent>> QueryEvents(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] string? eventSourceId = default,
        [FromQuery] string? eventTypeIds = default,
        [FromQuery] DateTimeOffset? occurredFrom = default,
        [FromQuery] DateTimeOffset? occurredTo = default,
        [FromQuery] bool descending = false)
    {
        var queryContext = _queryContextManager.Current;
        var paging = queryContext.Paging;

        var response = await _eventSequences.QueryEvents(new()
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
