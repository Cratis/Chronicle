// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

// TODO: Rename this to EventTypeGeneration.

/// <summary>
/// Represents the generation of an <see cref="EventType"/>.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventGeneration(uint Value) : ConceptAs<uint>(Value)
{
    /// <summary>
    /// Gets the underlying value of the first generation.
    /// </summary>
    public const uint FirstValue = 1U;

    /// <summary>
    /// Gets the definition of the first generation.
    /// </summary>
    public static readonly EventGeneration First = new(FirstValue);

    /// <summary>
    /// Gets the definition of the first generation.
    /// </summary>
    public static readonly EventGeneration Unspecified = new(uint.MaxValue);

    /// <summary>
    /// /// Implicitly convert from <see cref="uint"/> to <see cref="EventGeneration"/>.
    /// </summary>
    /// <param name="generation"><see cref="uint"/> to convert from.</param>
    public static implicit operator EventGeneration(uint generation) => new(generation);
}
