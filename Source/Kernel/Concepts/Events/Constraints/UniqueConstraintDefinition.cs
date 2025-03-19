// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventDefinitions">Collection of <see cref="UniqueConstraintEventDefinition"/>.</param>
/// <param name="RemovedWith">The <see cref="EventTypeId"/> of the event that removes the constraint.</param>
/// <param name="IgnoreCasing">Whether this constraint should ignore casing.</param>
public record UniqueConstraintDefinition(ConstraintName Name, IEnumerable<UniqueConstraintEventDefinition> EventDefinitions, EventTypeId? RemovedWith = default, bool IgnoreCasing = false) : IConstraintDefinition
{
    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => base.Equals(other);
}
