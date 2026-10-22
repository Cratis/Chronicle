// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the generation of a materialization - one isolated, complete build of a projection's read model.
/// </summary>
/// <remarks>
/// Generations are monotonic per projection. Generation 1 is the existing materialization adopted in place;
/// every later generation is built into its own destination. This is a different concept from
/// <see cref="ProjectionGeneration"/>, which identifies the generation of a projection's event type pins.
/// </remarks>
/// <param name="Value">Actual value.</param>
public record MaterializationGeneration(uint Value) : ConceptAs<uint>(Value)
{
    /// <summary>
    /// Gets the first generation - the existing materialization adopted in place.
    /// </summary>
    public static readonly MaterializationGeneration First = new(1U);

    /// <summary>
    /// Gets a value representing an unspecified generation.
    /// </summary>
    public static readonly MaterializationGeneration Unspecified = new(0U);

    /// <summary>
    /// Gets whether this is the first generation.
    /// </summary>
    public bool IsFirst => Value == First.Value;

    /// <summary>
    /// Implicitly convert from <see cref="uint"/> to <see cref="MaterializationGeneration"/>.
    /// </summary>
    /// <param name="value"><see cref="uint"/> to convert from.</param>
    public static implicit operator MaterializationGeneration(uint value) => new(value);

    /// <summary>
    /// Gets the generation following this one.
    /// </summary>
    /// <returns>The next <see cref="MaterializationGeneration"/>.</returns>
    public MaterializationGeneration Next() => new(checked(Value + 1));
}
