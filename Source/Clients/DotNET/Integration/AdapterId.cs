// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents the unique identifier of a projection.
/// </summary>
/// <param name="Value">The value.</param>
public record AdapterId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="AdapterId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator AdapterId(string value) => new(value);
}
