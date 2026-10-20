// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an OAuth client-credentials scope.
/// </summary>
/// <param name="Value">The scope requested from the authorization server.</param>
public record OAuthScope(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts a string to an OAuth scope.
    /// </summary>
    /// <param name="value">The scope.</param>
    public static implicit operator OAuthScope(string value) => new(value);
}
