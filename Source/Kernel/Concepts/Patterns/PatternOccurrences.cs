// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents how many times a pattern has been observed.
/// </summary>
/// <param name="Value">The actual value.</param>
public record PatternOccurrences(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Represents no occurrences at all.
    /// </summary>
    public static readonly PatternOccurrences None = new(0L);

    /// <summary>
    /// Implicitly convert from a long to <see cref="PatternOccurrences"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator PatternOccurrences(long value) => new(value);

    /// <summary>
    /// Implicitly convert from an int to <see cref="PatternOccurrences"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator PatternOccurrences(int value) => new((long)value);

    /// <summary>
    /// Increase the number of occurrences.
    /// </summary>
    /// <returns>A new instance with the increased count.</returns>
    public PatternOccurrences Increase() => new(Value + 1);
}
