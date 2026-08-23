// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Exception that gets thrown when multiple event types with the same id is found.
/// </summary>
public class MultipleEventTypesWithSameIdFound : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="MultipleEventTypesWithSameIdFound"/>.
    /// </summary>
    /// <param name="types">The CLR types.</param>
    public MultipleEventTypesWithSameIdFound(IEnumerable<Type> types)
        : base($"Multiple event types with the same id found: {string.Join(", ", types.Select(_ => _.FullName))}")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MultipleEventTypesWithSameIdFound"/> for a specific event type and generation.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> multiple CLR types represent.</param>
    /// <param name="types">The CLR types.</param>
    public MultipleEventTypesWithSameIdFound(EventType eventType, IEnumerable<Type> types)
        : base($"Multiple CLR types represent generation {eventType.Generation} of event type '{eventType.Id}': {string.Join(", ", types.Select(_ => _.Name))}. Each generation of an event type must be represented by exactly one type.")
    {
        EventType = eventType;
    }

    /// <summary>
    /// Gets the <see cref="EventType"/> multiple CLR types represent, if known.
    /// </summary>
    public EventType? EventType { get; }
}
