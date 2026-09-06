// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the share of all observed events a pattern was seen in, in the range 0 to 1.
/// </summary>
/// <param name="Value">The actual value.</param>
public record PatternSupport(double Value) : ConceptAs<double>(Value)
{
    /// <summary>
    /// Represents no support at all.
    /// </summary>
    public static readonly PatternSupport None = new(0d);

    /// <summary>
    /// Implicitly convert from a double to <see cref="PatternSupport"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator PatternSupport(double value) => new(value);
}
