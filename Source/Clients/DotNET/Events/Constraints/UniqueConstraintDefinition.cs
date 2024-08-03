// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventsWithProperties">The <see cref="EventType"/> and properties the constraint is for.</param>
public record UniqueConstraintDefinition(ConstraintName Name, Func<EventType, ConstraintViolationMessage> MessageCallback, IEnumerable<EventTypeAndProperty> EventsWithProperties) : IConstraintDefinition
{
    /// <inheritdoc/>
    public Constraint ToContract() => new()
    {
        Name = Name,
        Type = ConstraintType.Unique
    };
}
