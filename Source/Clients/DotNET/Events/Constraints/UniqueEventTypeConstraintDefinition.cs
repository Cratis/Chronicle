// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventType">The <see cref="EventType"/> the constraint is for.</param>
public record UniqueEventTypeConstraintDefinition(ConstraintName Name, Func<EventType, ConstraintViolationMessage> MessageCallback, EventType EventType) : IConstraintDefinition
{
    /// <inheritdoc/>
    public Constraint ToContract() => new()
    {
        Name = Name,
        Type = ConstraintType.UniqueEventType,
        Definition = new(new UniqueEventTypeConstraintDefinitionContract
        {
            EventType = EventType.ToContract()
        })
    };
}
