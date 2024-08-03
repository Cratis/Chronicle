// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a value used by a unique constraint.
/// </summary>
/// <param name="Value">The inner value.</param>
public record UniqueConstraintValue(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="UniqueConstraintValue"/>.
    /// </summary>
    /// <param name="name">Name to convert.</param>
    public static implicit operator UniqueConstraintValue(string name) => new(name);
}
