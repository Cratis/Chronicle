// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Expands projection clauses without changing event type discovery for other observers.
/// </summary>
internal static class ProjectionEventTypes
{
    /// <summary>
    /// Checks whether a type can be used by a projection clause under the given policy.
    /// </summary>
    /// <param name="type">The clause's CLR type.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <param name="generationDelivery">The projection's generation delivery policy.</param>
    /// <returns>Whether the type is an event type for this projection.</returns>
    internal static bool IsEventType(Type type, IEnumerable<Type> eventTypes, EventGenerationDelivery generationDelivery) =>
        type.IsEventType(eventTypes) ||
        (generationDelivery == EventGenerationDelivery.Pinned && Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute)));

    /// <summary>
    /// Expands a From clause, including a previous-generation type itself only for Pinned delivery.
    /// </summary>
    /// <param name="type">The clause's CLR type.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <param name="generationDelivery">The projection's effective generation delivery policy.</param>
    /// <returns>The CLR event types to register for the clause.</returns>
    internal static IEnumerable<Type> Expand(Type type, IEnumerable<Type> eventTypes, EventGenerationDelivery generationDelivery)
    {
        if (generationDelivery == EventGenerationDelivery.Pinned && Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute)))
        {
            yield return type;
        }

        foreach (var eventType in type.GetEventTypes(eventTypes))
        {
            yield return eventType;
        }
    }
}
