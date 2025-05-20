// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converters between <see cref="EventType"/> and its contract representation.
/// </summary>
internal static class EventTypeConverters
{
    /// <summary>
    /// Converts an <see cref="EventType"/> to a <see cref="Contracts.Events.EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static Contracts.Events.EventType ToContract(this EventType eventType) =>
        new()
        {
            Id = eventType.Id,
            Generation = eventType.Generation,
            Tombstone = eventType.Tombstone
        };

    /// <summary>
    /// Converts a collection of <see cref="EventType"/> to a <see cref="Contracts.Events.EventType"/>.
    /// </summary>
    /// <param name="eventTypes">The collection of event types to convert.</param>
    /// <returns>The converted collection of event types.</returns>
    public static IEnumerable<Contracts.Events.EventType> ToContract(this IEnumerable<EventType> eventTypes) =>
        eventTypes.Select(ToContract).ToArray();

    /// <summary>
    /// Converts a <see cref="Contracts.Events.EventType"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static EventType ToApi(this Contracts.Events.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.Tombstone);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Events.EventType"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventTypes">The collection of event types to convert.</param>
    /// <returns>The converted collection of event types.</returns>
    public static IEnumerable<EventType> ToApi(this IEnumerable<Contracts.Events.EventType> eventTypes) =>
        eventTypes.Select(ToApi).ToArray();
}
