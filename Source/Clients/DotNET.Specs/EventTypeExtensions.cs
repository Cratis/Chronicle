// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Extensions for <see cref="Contracts.Events.EventType"/>.
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>
    /// Determines whether two <see cref="Contracts.Events.EventType"/> are equal.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="other">The other event type.</param>
    /// <returns>True if they are equal, false otherwise.</returns>
    public static bool IsEqual(this Contracts.Events.EventType eventType, Contracts.Events.EventType other)
    {
        if (other is null) return false;
        if (ReferenceEquals(eventType, other)) return true;

        return
            eventType.Id == other.Id &&
            eventType.Generation == other.Generation &&
            eventType.Tombstone == other.Tombstone;
    }
}
