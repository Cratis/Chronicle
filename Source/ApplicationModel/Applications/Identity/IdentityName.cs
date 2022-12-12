// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Identity;

/// <summary>
/// Represents the name of the identity. Typically the username.
/// </summary>
/// <param name="Value">Concept value.</param>
public record IdentityName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="IdentityName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator IdentityName(string value) => new(value);
}
