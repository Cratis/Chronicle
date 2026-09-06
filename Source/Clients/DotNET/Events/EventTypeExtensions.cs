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
    /// Get the event store name for an event type.
    /// </summary>
    /// <remarks>
    /// First checks for a type-level <see cref="EventStoreAttribute"/>. If not found,
    /// falls back to an assembly-level <see cref="EventStoreAttribute"/> on the type's declaring assembly.
    /// </remarks>
    /// <param name="type">Type to get for.</param>
    /// <returns>The event store name, or <see langword="null"/> if neither the type nor its assembly has the attribute.</returns>
    public static string? GetEventStoreName(this Type type)
    {
        var typeAttribute = type.GetCustomAttribute<EventStoreAttribute>();
        if (typeAttribute is not null)
        {
            return typeAttribute.EventStore;
        }

        var assemblyAttribute = type.Assembly.GetCustomAttribute<EventStoreAttribute>();
        return assemblyAttribute?.EventStore;
    }

    /// <summary>
    /// Check if a type is an event type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is an event type, false if not.</returns>
    public static bool IsEventType(this Type type, IEnumerable<Type> eventTypes)
    {
        if (type == typeof(object))
        {
            return false;
        }

        if (Attribute.IsDefined(type, typeof(EventTypeAttribute)))
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

        if (Attribute.IsDefined(type, typeof(EventTypeAttribute)))
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
    /// <exception cref="TypeIsNotAnEventType">Thrown if the type is neither marked with <see cref="EventTypeAttribute"/> nor <see cref="EventTypeGenerationForAttribute"/>.</exception>
    /// <exception cref="EventTypeGenerationForCannotBeCombinedWithEventType">Thrown if the type is marked with both attributes.</exception>
    public static void ValidateEventType(this Type type)
    {
        var hasEventType = Attribute.IsDefined(type, typeof(EventTypeAttribute));
        var hasGenerationFor = Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute));

        if (hasEventType && hasGenerationFor)
        {
            throw new EventTypeGenerationForCannotBeCombinedWithEventType(type);
        }

        if (!hasEventType && !hasGenerationFor)
        {
            throw new TypeIsNotAnEventType(type);
        }
    }

    /// <summary>
    /// Get the <see cref="EventType"/> for a CLR type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get for. </param>
    /// <returns>The <see cref="EventType"/>.</returns>
    /// <exception cref="TypeIsNotAnEventType">Thrown if the type is not an event type.</exception>
    /// <exception cref="EventTypeGenerationForCannotBeCombinedWithEventType">Thrown if the type is marked with both <see cref="EventTypeAttribute"/> and <see cref="EventTypeGenerationForAttribute"/>.</exception>
    /// <exception cref="EventTypeGenerationReferencesNonEventType">Thrown if a <see cref="EventTypeGenerationForAttribute"/> references a type that is not marked with <see cref="EventTypeAttribute"/>.</exception>
    public static EventType GetEventType(this Type type)
    {
        type.ValidateEventType();

        if (type.GetCustomAttribute<EventTypeGenerationForAttribute>() is { } generationFor)
        {
            var referencedType = generationFor.EventTypeClrType;
            var referencedAttribute = referencedType.GetCustomAttribute<EventTypeAttribute>() ??
                throw new EventTypeGenerationReferencesNonEventType(type, referencedType);

            return new EventType(ResolveId(referencedType, referencedAttribute), generationFor.Generation);
        }

        var attribute = type.GetCustomAttribute<EventTypeAttribute>()!;
        return new EventType(ResolveId(type, attribute), attribute.Generation);
    }

    static EventTypeId ResolveId(Type type, EventTypeAttribute attribute) => attribute.Id.Value switch
    {
        "" => type.Name,
        _ => attribute.Id.Value
    };
}
