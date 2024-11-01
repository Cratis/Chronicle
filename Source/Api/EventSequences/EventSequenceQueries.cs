// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Applications.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Api.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with the event log.</param>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/> for managing query contexts.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
[Route("/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequenceQueries(IStorage storage, IQueryContextManager queryContextManager, JsonSerializerOptions jsonSerializerOptions) : ControllerBase
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
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId) =>
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
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId) =>
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
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
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
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<AppendedEventWithJsonAsContent>> AppendedEvents(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] EventSourceId? eventSourceId = null!)
    {
        var queryContext = queryContextManager.Current;

        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

        var result = new List<AppendedEventWithJsonAsContent>();

        var tail = await eventSequence.GetTailSequenceNumber();
        queryContext.TotalItems = (int)tail.Value;

        var from = EventSequenceNumber.First + (queryContext.Paging.Page * queryContext.Paging.Size);

        IEventCursor cursor;

        if (queryContext.Paging.IsPaged)
        {
            cursor = await eventSequence.GetRange(
                start: from,
                end: from + (queryContext.Paging.Size - 1),
                eventSourceId);
        }
        else
        {
            cursor = await eventSequence.GetFromSequenceNumber(from, eventSourceId);
        }

        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current.Select(_ => new AppendedEventWithJsonAsContent(
                _.Metadata,
                _.Context,
                JsonSerializer.SerializeToNode(_.Content, jsonSerializerOptions)!)));
        }
        return result;
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
    public async Task<IQueryable<AppendedEventWithJsonAsContent>> AppendedEventsInRange(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] EventSequenceNumber fromSequenceNumber,
        [FromQuery] EventSequenceNumber toSequenceNumber,
        [FromQuery] EventSourceId? eventSourceId = null!)
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
