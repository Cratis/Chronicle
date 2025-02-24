// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents the unique identifier of a reducer.
/// </summary>
/// <param name="Value">The actual value.</param>
public record ReducerId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReducerId"/>.
    /// </summary>
    public static readonly ReducerId Unspecified = "[unspecified]";

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReducerId(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="ReducerId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReducerId"/> to convert from.</param>
    public static implicit operator ObserverId(ReducerId id) => new(id.Value);
}
