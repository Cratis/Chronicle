// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Describes an event target without enabling publication.
/// </summary>
/// <param name="EventType">The exact registered event type, including its generation.</param>
/// <param name="EventSequence">The destination sequence; omission selects the outbox, never the event log.</param>
/// <param name="IsPublic">Whether the client declared the event type public. Only public event types may be published to the outbox.</param>
public record EventSequenceSinkConfiguration(EventType EventType, EventSequenceId? EventSequence = null, bool IsPublic = false)
{
    /// <summary>
    /// Gets the explicit destination for publication.
    /// </summary>
    public EventSequenceId Destination => EventSequence ?? EventSequenceId.Outbox;

    /// <summary>
    /// Ensures the destination is one an event target may publish to.
    /// </summary>
    /// <exception cref="EventLogIsNotAPublicationTarget">Thrown when the destination is the event log.</exception>
    /// <exception cref="PrivateEventCannotBePublishedToOutbox">Thrown when the outbox is the destination and the event type is not public.</exception>
    public void EnsureDestinationAllowed()
    {
        if (Destination == EventSequenceId.Log)
        {
            throw new EventLogIsNotAPublicationTarget(EventType);
        }

        if (Destination == EventSequenceId.Outbox && !IsPublic)
        {
            throw new PrivateEventCannotBePublishedToOutbox(EventType);
        }
    }

    /// <summary>
    /// Resolves the target from registered schemas without allocating a generation.
    /// </summary>
    /// <param name="schemas">The registered event schemas.</param>
    /// <returns>The schema and identity of the exact registered target.</returns>
    /// <exception cref="MissingEventTargetSchema">Thrown when the exact event type is not registered.</exception>
    public EventTypeSchema ResolveTarget(IEnumerable<EventTypeSchema> schemas) =>
        schemas.SingleOrDefault(schema => schema.Type == EventType) ?? throw new MissingEventTargetSchema(EventType);
}
