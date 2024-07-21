// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents the unique identifier of a reducer.
/// </summary>
/// <param name="value">The actual value.</param>
public record ReducerId(string value) : ConceptAs<string>(value)
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
}
