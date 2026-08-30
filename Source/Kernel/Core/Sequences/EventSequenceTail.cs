// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the sequence number of the tail event in an event sequence.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the tail event.</param>
/// <remarks>
/// Declared as a read model for the same reason as <see cref="EventSourceEvents"/>: a query returns its own read
/// model, and the value travels on the wire as the unwrapped primitive either way.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record EventSequenceTail(EventSequenceNumber SequenceNumber)
{
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
    public static async Task<EventSequenceTail> TailSequenceNumber(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
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

        return new(tail);
    }
}
