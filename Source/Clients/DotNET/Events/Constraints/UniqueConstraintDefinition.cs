// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventsWithProperties">The <see cref="EventType"/> and properties the constraint is for.</param>
public record UniqueConstraintDefinition(ConstraintName Name, ConstraintViolationMessageProvider MessageCallback, IEnumerable<UniqueConstraintEventDefinition> EventsWithProperties) : IConstraintDefinition
{
    /// <inheritdoc/>
    public Constraint ToContract() => new()
    {
        Name = Name,
        Type = ConstraintType.Unique,
        Definition = new(new UniqueConstraintDefinitionContract
        {
            EventDefinitions = EventsWithProperties.Select(_ => new Contracts.Events.Constraints.UniqueConstraintEventDefinition
            {
                EventType = _.EventType.ToContract(),
                Property = _.Property
            }).ToList()
        })
    };
}
