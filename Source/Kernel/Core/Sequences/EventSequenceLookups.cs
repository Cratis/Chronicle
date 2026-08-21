// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Home for event sequence lookups that answer with a single value computed on demand from storage - not a
/// projected read model, and not naturally the shape of any other query in this service.
/// </summary>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record EventSequenceLookups
{
    /// <summary>
    /// Checks whether an event source has any events in an event sequence.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventStore">Event store to check in.</param>
    /// <param name="namespace">Namespace to check in.</param>
    /// <param name="eventSequenceId">Event sequence to check in.</param>
    /// <param name="eventSourceId">The event source to check for.</param>
    /// <returns>True if the event source has events, false otherwise.</returns>
    public static Task<bool> HasEventsForEventSourceId(
        IStorage storage,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string eventSourceId) =>
        storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId).HasEventsFor(eventSourceId);

    /// <summary>
    /// Gets the sequence number of the tail event in an event sequence, optionally narrowed.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventSourceType">Optional event source type to narrow to.</param>
    /// <param name="eventStreamId">Optional event stream to narrow to.</param>
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <returns>The tail sequence number.</returns>
    public static async Task<ulong> TailSequenceNumber(
        IStorage storage,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string? eventTypeIds = default,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamId = default,
        string? eventStreamType = default)
    {
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);

        Concepts.Events.EventSourceId? resolvedEventSourceId = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventSourceId) is { } trimmedEventSourceId)
        {
            resolvedEventSourceId = trimmedEventSourceId;
        }

        Concepts.Events.EventSourceType? resolvedEventSourceType = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventSourceType) is { } trimmedEventSourceType)
        {
            resolvedEventSourceType = trimmedEventSourceType;
        }

        Concepts.Events.EventStreamId? resolvedEventStreamId = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventStreamId) is { } trimmedEventStreamId)
        {
            resolvedEventStreamId = trimmedEventStreamId;
        }

        Concepts.Events.EventStreamType? resolvedEventStreamType = null;
        if (EventSequenceQueryCriteriaFactory.Trimmed(eventStreamType) is { } trimmedEventStreamType)
        {
            resolvedEventStreamType = trimmedEventStreamType;
        }

        var tail = await eventSequence.GetTailSequenceNumber(
            EventSequenceQueryCriteriaFactory.SplitEventTypes(eventTypeIds),
            resolvedEventSourceId,
            resolvedEventSourceType,
            resolvedEventStreamId,
            resolvedEventStreamType);

        return tail.Value;
    }
}
