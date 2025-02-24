// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Concept that represents a partition.
/// </summary>
/// <param name="Value">Actual value.</param>
public record Partition(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from a string to <see cref="Partition"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator Partition(string value) => new(value);
}
