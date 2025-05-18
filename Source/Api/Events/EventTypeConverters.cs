// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converts between <see cref="Contracts.Events.EventType"/> and <see cref="EventType"/>.
/// </summary>
public static class EventTypeConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Events.EventType"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static EventType ToApi(this Contracts.Events.EventType eventType) => new(
        eventType.Id,
        eventType.Generation,
        eventType.Tombstone);

    /// <summary>
    /// Converts an <see cref="EventType"/> to a <see cref="Contracts.Events.EventType"/>.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static Contracts.Events.EventType ToContract(this EventType eventType) => new()
    {
        Id = eventType.Id,
        Generation = eventType.Generation,
        Tombstone = eventType.Tombstone
    };
}
