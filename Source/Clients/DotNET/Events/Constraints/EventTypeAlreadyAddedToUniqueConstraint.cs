// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an event type has already been added to the unique constraint.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventTypeAlreadyAddedToUniqueConstraint"/>.
/// </remarks>
/// <param name="constraintName">The name of the constraint.</param>
/// <param name="eventType">The event type that is duplicate.</param>
/// <param name="property">The property in the definition.</param>
public class EventTypeAlreadyAddedToUniqueConstraint(ConstraintName constraintName, EventType eventType, string property)
    : Exception($"The event type '{eventType}' with property '{property}' has already been added to the unique constraint with name '{constraintName}'")
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public EventType EventType { get; } = eventType;

    /// <summary>
    /// Gets the property.
    /// </summary>
    public string Property { get; } = property;
}
