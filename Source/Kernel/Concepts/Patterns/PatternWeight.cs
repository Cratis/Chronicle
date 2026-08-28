// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the recency-weighted strength of a pattern.
/// </summary>
/// <param name="Value">The actual value.</param>
/// <remarks>
/// Unlike <see cref="PatternOccurrences"/>, weight decays as a pattern goes unseen, so a behavior somebody stopped
/// months ago sinks below the threshold and is pruned instead of competing forever with what they do now.
/// </remarks>
public record PatternWeight(double Value) : ConceptAs<double>(Value)
{
    /// <summary>
    /// Represents no weight at all.
    /// </summary>
    public static readonly PatternWeight None = new(0d);

    /// <summary>
    /// Implicitly convert from a double to <see cref="PatternWeight"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator PatternWeight(double value) => new(value);
}
