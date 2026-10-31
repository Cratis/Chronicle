// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a token for initial administrator setup.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record InitialAdminSetupToken(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="InitialAdminSetupToken"/>.
    /// </summary>
    public static readonly InitialAdminSetupToken Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="InitialAdminSetupToken"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted token.</returns>
    public static implicit operator InitialAdminSetupToken(string value) => new(value);
}
