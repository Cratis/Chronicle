// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a name for a constraint.
/// </summary>
/// <param name="Name">The inner value.</param>
public record ConstraintName(string Name) : ConceptAs<string>(Name)
{
    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="ConstraintName"/>.
    /// </summary>
    /// <param name="name">Name to convert.</param>
    public static implicit operator ConstraintName(string name) => new(name);
}
