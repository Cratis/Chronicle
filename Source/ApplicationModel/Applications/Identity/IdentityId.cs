// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Identity;

/// <summary>
/// Represents the unique identifier for the identity owned by the identity provider.
/// </summary>
/// <param name="Value">Concept value.</param>
public record IdentityId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="IdentityId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator IdentityId(string value) => new(value);
}
