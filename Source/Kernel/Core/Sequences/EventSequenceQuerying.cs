// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Shared logic behind every event sequence read - paging through storage and releasing compliance-protected
/// content - so <see cref="AppendedEvent"/> and <see cref="ExportedEvent"/> do not each carry their own copy.
/// </summary>
internal static class EventSequenceQuerying
{
    /// <summary>
    /// Reads a page of events matching criteria from an event sequence, with PII content released.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="eventStore">The event store the sequence belongs to.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="eventSequenceId">The event sequence to read.</param>
    /// <param name="criteria">The <see cref="EventSequenceQueryCriteria"/> narrowing the read.</param>
    /// <param name="skip">The number of matching events to skip.</param>
    /// <param name="take">The number of matching events to take.</param>
    /// <param name="sort">How the page is ordered.</param>
    /// <returns>The matching events for the page, with the total count of events matching the criteria.</returns>
    internal static async Task<(IEnumerable<Concepts.Events.AppendedEvent> Events, ulong TotalCount)> QueryPage(
        IStorage storage,
        IEventCompliance eventCompliance,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        EventSequenceQueryCriteria criteria,
        int skip,
        int take,
        EventSequenceQuerySort sort)
    {
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
        var totalCount = await eventSequence.GetCountMatching(criteria);

        var appendedEvents = new List<Concepts.Events.AppendedEvent>();
        using (var cursor = await eventSequence.GetPage(criteria, skip, take, sort))
        {
            while (await cursor.MoveNext())
            {
                appendedEvents.AddRange(cursor.Current);
            }
        }

        var released = await ReleaseCompliance(appendedEvents, storage, eventStore, eventCompliance);
        return (released, totalCount);
    }

    /// <summary>
    /// Reads every event from a specific sequence number onward, optionally narrowed to an event source, event
    /// stream, and event types, with PII content released.
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
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <param name="eventStreamId">Optional event stream to narrow to.</param>
    /// <returns>Every matching event, unpaged.</returns>
    internal static async Task<IEnumerable<AppendedEvent>> ReadFromSequenceNumber(
        IStorage storage,
        IEventCompliance eventCompliance,
        JsonSerializerOptions jsonSerializerOptions,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        Concepts.Events.EventSequenceNumber fromEventSequenceNumber,
        string? eventSourceId = default,
        string? eventTypeIds = default,
        string? eventStreamType = default,
        string? eventStreamId = default)
    {
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

        Concepts.Events.EventSourceId? resolvedEventSourceId = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventSourceId) is { } trimmedEventSourceId)
        {
            resolvedEventSourceId = trimmedEventSourceId;
        }

        Concepts.Events.EventStreamType? resolvedEventStreamType = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventStreamType) is { } trimmedEventStreamType)
        {
            resolvedEventStreamType = trimmedEventStreamType;
        }

        Concepts.Events.EventStreamId? resolvedEventStreamId = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventStreamId) is { } trimmedEventStreamId)
        {
            resolvedEventStreamId = trimmedEventStreamId;
        }

        var appendedEvents = new List<Concepts.Events.AppendedEvent>();
        using (var cursor = await eventSequence.GetFromSequenceNumber(
            fromEventSequenceNumber,
            resolvedEventSourceId,
            resolvedEventStreamType,
            resolvedEventStreamId,
            EventSequenceQueryCriteriaFactory.SplitEventTypes(eventTypeIds)))
        {
            while (await cursor.MoveNext())
            {
                appendedEvents.AddRange(cursor.Current);
            }
        }

        var released = await ReleaseCompliance(appendedEvents, storage, eventStore, eventCompliance);
        return released.ToApi(jsonSerializerOptions);
    }

    /// <summary>
    /// Releases PII content in a collection of events, using each event's own schema.
    /// </summary>
    /// <param name="events">The events to release.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read event type schemas from.</param>
    /// <param name="eventStore">The event store the events belong to.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release with.</param>
    /// <returns>The events, with PII content decrypted where applicable.</returns>
    internal static async Task<IEnumerable<Concepts.Events.AppendedEvent>> ReleaseCompliance(
        IEnumerable<Concepts.Events.AppendedEvent> events,
        IStorage storage,
        string eventStore,
        IEventCompliance eventCompliance)
    {
        var materialized = events as ICollection<Concepts.Events.AppendedEvent> ?? events.ToList();
        var eventTypeSchemas = await storage.GetEventStore(eventStore).EventTypes.GetFor(materialized.Select(e => e.Context.EventType).Distinct());
        var schemasByEventType = eventTypeSchemas.ToDictionary(schema => schema.Type);
        return await eventCompliance.Release(materialized, schemasByEventType);
    }
}
