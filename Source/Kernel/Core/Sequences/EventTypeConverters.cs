// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converters between <see cref="EventType"/> and its contract and storage representations.
/// </summary>
internal static class EventTypeConverters
{
    /// <summary>
    /// Converts an <see cref="EventType"/> to a <see cref="Contracts.Sequences.EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static Contracts.Sequences.EventType ToContract(this EventType eventType) =>
        new()
        {
            Id = eventType.Id,
            Generation = eventType.Generation,
            Tombstone = eventType.Tombstone
        };

    /// <summary>
    /// Converts a <see cref="Contracts.Sequences.EventType"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static EventType ToApi(this Contracts.Sequences.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.Tombstone);

    /// <summary>
    /// Converts a storage <see cref="Concepts.Events.EventType"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static EventType ToApi(this Concepts.Events.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.Tombstone);

    /// <summary>
    /// Converts an <see cref="EventType"/> to a storage <see cref="Concepts.Events.EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static Concepts.Events.EventType ToChronicle(this EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.Tombstone);
}
