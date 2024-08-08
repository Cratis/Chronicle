// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an event type has already been added to a unique constraint.
/// </summary>
/// <param name="eventType">The <see cref="EventType"/> that is being added for.</param>
/// <param name="property">The property on event.</param>
public class PropertyDoesNotExistOnEventType(EventType eventType, string property)
    : Exception($"Property '{property}' does not exist on event type '{eventType}'")
{
    /// <summary>
    /// Gets the <see cref="EventType"/> that is being added for.
    /// </summary>
    public EventType EventType => eventType;

    /// <summary>
    /// Gets the property on event.
    /// </summary>
    public string Property => property;
}
