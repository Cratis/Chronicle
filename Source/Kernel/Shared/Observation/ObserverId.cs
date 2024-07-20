// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Concept that represents the unique identifier of an observer.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ObserverId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ObserverId"/>.
    /// </summary>
    public static readonly ObserverId Unspecified = new("[unspecified]");

    /// <summary>
    /// Implicitly convert from a string to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ObserverId(string id) => new(id);
}
