// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an OAuth authority URL.
/// </summary>
/// <param name="Value">The actual value.</param>
public record Authority(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="Authority"/>.
    /// </summary>
    public static readonly Authority Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="Authority"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator Authority(string value) => new(value);
}
