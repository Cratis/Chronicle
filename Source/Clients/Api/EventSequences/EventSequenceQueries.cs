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
            ToEventSequenceNumber = queryContext.Paging.IsPaged ? from + (ulong)(queryContext.Paging.Size - 1) : null!,
            EventSourceId = eventSourceId
        });

        return response.Events.ToApi();
    }
}
