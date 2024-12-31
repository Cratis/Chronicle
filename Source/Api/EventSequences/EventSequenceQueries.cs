// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Applications.Queries;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Api.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <param name="eventSequences"><see cref="IEventSequences"/> service for working with the event log.</param>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/> for managing query contexts.</param>
[Route("/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequenceQueries(IEventSequences eventSequences, IQueryContextManager queryContextManager) : ControllerBase
{
    /// <summary>
    /// Get the head sequence number.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <returns>The tail sequence number.</returns>
    [HttpGet("next-sequence-number")]
    public Task<ulong> Next(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get the tail sequence number.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <returns>The tail sequence number.</returns>
    [HttpGet("tail-sequence-number")]
    public Task<ulong> Tail(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId) =>
        throw new NotImplementedException();

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
        [FromQuery] string? eventSourceId = null!)
    {
        var queryContext = queryContextManager.Current;

        var tail = await eventSequences.GetTailSequenceNumber(new()
        {
            EventStoreName = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId
        });
        queryContext.TotalItems = (int)tail.SequenceNumber;

        var from = (ulong)(queryContext.Paging.Page * queryContext.Paging.Size);
        var response = await eventSequences.GetEventsFromEventSequenceNumber(new()
        {
            EventStoreName = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            FromEventSequenceNumber = from,
            ToEventSequenceNumber = queryContext.Paging.IsPaged ? from + (ulong)(queryContext.Paging.Size - 1) : null!,
            EventSourceId = eventSourceId ?? null!
        });

        return response.Events;
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="fromSequenceNumber">Fetch events from this id, including.</param>
    /// <param name="toSequenceNumber">Fetch events to this id, excluding.</param>
    /// <param name="eventSourceId">Optional event source id to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet("range")]
    public async Task<IQueryable<AppendedEvent>> AppendedEventsInRange(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] ulong fromSequenceNumber,
        [FromQuery] ulong toSequenceNumber,
        [FromQuery] string? eventSourceId = null!)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get a histogram of a specific event sequence. PS: Not implemented yet.
    /// </summary>
    /// <returns>A collection of <see cref="EventHistogramEntry"/>.</returns>
    [HttpGet("histogram")]
    public Task<IEnumerable<EventHistogramEntry>> Histogram(/*[FromRoute] EventSequenceId eventSequenceId*/) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());
}
