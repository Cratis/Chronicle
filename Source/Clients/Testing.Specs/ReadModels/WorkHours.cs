// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents the number of hours worked, as a strongly-typed <see cref="ConceptAs{T}"/> over a
/// <see cref="decimal"/>. Used to verify that a child collection keyed by a value-type primitive
/// still materializes when the child carries a concept-typed value.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkHours(decimal Value) : ConceptAs<decimal>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="decimal"/> to a <see cref="WorkHours"/>.
    /// </summary>
    /// <param name="value">The <see cref="decimal"/> to convert.</param>
    public static implicit operator WorkHours(decimal value) => new(value);
}
