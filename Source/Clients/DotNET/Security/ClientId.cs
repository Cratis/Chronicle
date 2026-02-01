// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an OAuth client identifier.
/// </summary>
/// <param name="Value">The actual value.</param>
public record ClientId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="ClientId"/>.
    /// </summary>
    public static readonly ClientId Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="ClientId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator ClientId(string value) => new(value);
}
