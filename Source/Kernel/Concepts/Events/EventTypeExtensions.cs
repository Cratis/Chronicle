// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Extension methods for working with <see cref="EventType"/> and <see cref="Type"/> .
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>
    /// Check if a type is an event type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is an event type, false if not.</returns>
    public static bool IsEventType(this Type type)
    {
        if (type == typeof(object))
        {
            return false;
        }

        return type.GetCustomAttribute<EventTypeAttribute>() != null;
    }

    /// <summary>
    /// Gets the event type from the reactor method.
    /// </summary>
    /// <param name="eventType">The type to get from.</param>
    /// <returns>The event type.</returns>
    public static EventType GetEventType(this Type eventType)
    {
        var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>()!;
        return new EventType(eventType.Name, eventTypeAttribute.Generation);
    }
}
