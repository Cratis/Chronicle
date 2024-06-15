// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequence"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
[Route("/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequence(
    IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Get the head sequence number.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <returns>The tail sequence number.</returns>
    [HttpGet("next-sequence-number")]
    public Task<EventSequenceNumber> Next(
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
    public Task<EventSequenceNumber> Tail(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get the tail sequence number for a specific observer.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="observerId">The observer to get for.</param>
    /// <returns>The tail sequence number.</returns>
    /// <remarks>
    /// This will use the event types of the observer to determine the tail sequence number of
    /// the last event that was appended to the event sequence that the observer is interested in.
    /// </remarks>
    [HttpGet("tail-sequence-number/observer/{observerId}")]
    public async Task<EventSequenceNumber> TailForObserver(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromRoute] string observerId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="pageSize">Size of page to return.</param>
    /// <param name="pageNumber">Page number to return.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<PagedQueryResult<AppendedEventWithJsonAsContent>> GetAppendedEvents(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] int pageSize = 100,
        [FromQuery] int pageNumber = 0,
        [FromQuery] string eventSourceId = null!)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="fromSequenceNumber">Fetch events from this id, including.</param>
    /// <param name="toSequenceNumber">Fetch events to this id, excluding.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet("range")]
    public async Task<PagedQueryResult<AppendedEventWithJsonAsContent>> GetAppendedEventsInRange(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] ulong fromSequenceNumber,
        [FromQuery] ulong toSequenceNumber,
        [FromQuery] string eventSourceId = null!)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet("all")]
    public async Task<IEnumerable<AppendedEventWithJsonAsContent>> GetAllAppendedEvents(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromQuery] string eventSourceId = null!)
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
