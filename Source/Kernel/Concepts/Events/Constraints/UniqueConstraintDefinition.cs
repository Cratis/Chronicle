// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// /// <param name="EventDefinitions">Collection of <see cref="UniqueConstraintEventDefinition"/>.</param>
public record UniqueConstraintDefinition(ConstraintName Name, IEnumerable<UniqueConstraintEventDefinition> EventDefinitions) : IConstraintDefinition
{
    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => base.Equals(other);
}
