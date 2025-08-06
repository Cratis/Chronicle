// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the generation of a read model.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ReadModelGeneration(uint Value) : ConceptAs<uint>(Value)
{
    /// <summary>
    /// Gets the underlying value of the first generation.
    /// </summary>
    public const uint FirstValue = 1U;

    /// <summary>
    /// Gets the definition of the first generation.
    /// </summary>
    public static readonly ReadModelGeneration First = new(FirstValue);

    /// <summary>
    /// Gets the definition of the first generation.
    /// </summary>
    public static readonly ReadModelGeneration Unspecified = new(uint.MaxValue);

    /// <summary>
    /// Implicitly convert from <see cref="uint"/> to <see cref="ReadModelGeneration"/>.
    /// </summary>
    /// <param name="generation"><see cref="uint"/> to convert from.</param>
    public static implicit operator ReadModelGeneration(uint generation) => new(generation);

    /// <summary>
    /// Implicitly convert from <see cref="ReadModelGeneration"/> to string.
    /// </summary>
    /// <param name="generation"><see cref="ReadModelGeneration"/> to convert from.</param>
    public static implicit operator ReadModelGeneration(string generation)
    {
        if (string.IsNullOrEmpty(generation))
        {
            return Unspecified;
        }

        if (uint.TryParse(generation, out var value))
        {
            return new(value);
        }

        return Unspecified;
    }
}
