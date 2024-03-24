// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;
using Microsoft.AspNetCore.Mvc;
using IEventSequence = Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Cratis.API.EventSequences.Queries;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequence"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
[Route("/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequence(
    IStorage storage,
    IGrainFactory grainFactory,
    JsonSerializerOptions jsonSerializerOptions) : ControllerBase
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
        GetEventSequence(eventStore, @namespace, eventSequenceId).GetNextSequenceNumber();

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
        GetEventSequence(eventStore, @namespace, eventSequenceId).GetTailSequenceNumber();

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
        [FromRoute] ObserverId observerId)
    {
        var observer = await GetObserverStorage(eventStore, @namespace).GetObserver(observerId);
        return await GetEventSequenceStorage(eventStore, @namespace, eventSequenceId).GetTailSequenceNumber(observer.EventTypes);
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
    /// <param name="eventTypes">Optional collection of <see cref="EventType"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<PagedQueryResult<AppendedEventWithJsonAsContent>> GetAppendedEvents(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] int pageSize = 100,
        [FromQuery] int pageNumber = 0,
        [FromQuery] EventSourceId eventSourceId = null!,
        [FromQuery(Name = "eventTypes[]")] IEnumerable<string> eventTypes = null!)
    {
        var result = new List<AppendedEventWithJsonAsContent>();
        var parsedEventTypes = eventTypes?.Select(EventType.Parse).ToArray();

        var eventSequenceStorage = GetEventSequenceStorage(eventStore, @namespace, eventSequenceId);

        var from = EventSequenceNumber.First + (pageNumber * pageSize);
        var tail = await eventSequenceStorage.GetTailSequenceNumber(
            eventTypes: parsedEventTypes,
            eventSourceId: eventSourceId);

        if (tail == EventSequenceNumber.Unavailable)
        {
            return new(Enumerable.Empty<AppendedEventWithJsonAsContent>(), 0);
        }

        var cursor = await eventSequenceStorage.GetRange(
            start: from,
            end: from + (pageSize - 1),
            eventSourceId: eventSourceId,
            eventTypes: parsedEventTypes);
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current.Select(_ => new AppendedEventWithJsonAsContent(
                _.Metadata,
                _.Context,
                JsonSerializer.SerializeToNode(_.Content, jsonSerializerOptions)!)));
        }
        return new(result, tail);
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
    /// <param name="eventTypes">Optional collection of <see cref="EventType"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet("range")]
    public async Task<PagedQueryResult<AppendedEventWithJsonAsContent>> GetAppendedEventsRange(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] ulong fromSequenceNumber,
        [FromQuery] ulong toSequenceNumber,
        [FromQuery] EventSourceId eventSourceId = null!,
        [FromQuery(Name = "eventTypes[]")] IEnumerable<string> eventTypes = null!)
    {
        var result = new List<AppendedEventWithJsonAsContent>();
        var parsedEventTypes = eventTypes?.Select(EventType.Parse).ToArray();
        var eventSequenceStorage = GetEventSequenceStorage(eventStore, @namespace, eventSequenceId);
        var tail = await eventSequenceStorage.GetTailSequenceNumber(
            eventTypes: parsedEventTypes,
            eventSourceId: eventSourceId);

        if (tail == EventSequenceNumber.Unavailable)
        {
            return new(Enumerable.Empty<AppendedEventWithJsonAsContent>(), 0);
        }

        var cursor = await eventSequenceStorage.GetRange(
            start: fromSequenceNumber,
            end: toSequenceNumber,
            eventSourceId: eventSourceId,
            eventTypes: parsedEventTypes);
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current.Select(_ => new AppendedEventWithJsonAsContent(
                _.Metadata,
                _.Context,
                JsonSerializer.SerializeToNode(_.Content, jsonSerializerOptions)!)));
        }
        return new(result, tail);
    }

    /// <summary>
    /// Get events for a specific event sequence in an event store in a specific namespace.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet("all")]
    public async Task<IEnumerable<AppendedEventWithJsonAsContent>> GetAllAppendedEvents(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] EventSourceId eventSourceId = null!,
        [FromQuery(Name = "eventTypes[]")] IEnumerable<string> eventTypes = null!)
    {
        var result = new List<AppendedEventWithJsonAsContent>();
        var parsedEventTypes = eventTypes?.Select(EventType.Parse).ToArray();

        var cursor = await GetEventSequenceStorage(eventStore, @namespace, eventSequenceId).GetFromSequenceNumber(
            sequenceNumber: EventSequenceNumber.First,
            eventSourceId: eventSourceId,
            eventTypes: parsedEventTypes);
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
    /// Get a histogram of a specific event sequence. PS: Not implemented yet.
    /// </summary>
    /// <returns>A collection of <see cref="EventHistogramEntry"/>.</returns>
    [HttpGet("histogram")]
    public Task<IEnumerable<EventHistogramEntry>> Histogram(/*[FromRoute] EventSequenceId eventSequenceId*/) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());

    IEventSequenceStorage GetEventSequenceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) => storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
    IObserverStorage GetObserverStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace) => storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers;
    IEventSequence GetEventSequence(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new EventSequenceKey(eventStore, @namespace));
}
