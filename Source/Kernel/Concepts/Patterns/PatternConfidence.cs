// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents how often a pattern holds when the context it constrains is present, in the range 0 to 1.
/// </summary>
/// <param name="Value">The actual value.</param>
public record PatternConfidence(double Value) : ConceptAs<double>(Value)
{
    /// <summary>
    /// Represents no confidence at all.
    /// </summary>
    public static readonly PatternConfidence None = new(0d);

    /// <summary>
    /// Represents full confidence.
    /// </summary>
    public static readonly PatternConfidence Certain = new(1d);

    /// <summary>
    /// Implicitly convert from a double to <see cref="PatternConfidence"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator PatternConfidence(double value) => new(value);
}
