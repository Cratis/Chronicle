// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an event type has already been added to a unique constraint.
/// </summary>
/// <param name="eventType"><see cref="EventType"/> that is being added for.</param>
/// <param name="property">Property on event.</param>
public class PropertyTypeMismatchInUniqueConstraint(EventType eventType, string property)
    : Exception($"Property '{property}' on event type '{eventType}' mismatches with existing properties in unique constraint.")
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
