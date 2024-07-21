// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Extension methods for working with <see cref="EventType"/> and <see cref="Type"/> .
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>
    /// Check if a type is an event type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is an event type, false if not.</returns>
    public static bool IsEventType(this Type type, IEnumerable<Type> eventTypes)
    {
        if (type.GetCustomAttribute<EventTypeAttribute>() != null)
        {
            return true;
        }

        return eventTypes.Any(_ => _.IsAssignableTo(type));
    }

    /// <summary>
    /// Get the event types for a type.
    /// </summary>
    /// <param name="type">The CLR type to get for.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>Collection of actual event types.</returns>
    public static IEnumerable<Type> GetEventTypes(this Type type, IEnumerable<Type> eventTypes)
    {
        eventTypes = eventTypes.Except([type]);

        if (type.GetCustomAttribute<EventTypeAttribute>() != null)
        {
            yield return type;
        }

        foreach (var eventType in eventTypes)
        {
            if (eventType.IsAssignableTo(type))
            {
                yield return eventType;
            }
        }
    }

    /// <summary>
    /// Validate if a type is an event type.
    /// </summary>
    /// <param name="type">Type to validate.</param>
    /// <exception cref="MissingEventTypeAttribute">Thrown if type does not have the <see cref="EventTypeAttribute"/>.</exception>
    public static void ValidateEventType(this Type type)
    {
        if (type.GetCustomAttribute<EventTypeAttribute>() == null)
        {
            throw new MissingEventTypeAttribute(type);
        }
    }

    /// <summary>
    /// Get the <see cref="EventType"/> for a CLR type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get for. </param>
    /// <returns>The <see cref="EventType"/>.</returns>
    public static EventType GetEventType(this Type type)
    {
        var attribute = type.GetCustomAttribute<EventTypeAttribute>()!;
        var id = attribute.Id.Value switch
        {
            "" => type.Name,
            _ => attribute.Id.Value
        };
        return new EventType(new EventTypeId(id), attribute.Generation, attribute.IsPublic);
    }
}
