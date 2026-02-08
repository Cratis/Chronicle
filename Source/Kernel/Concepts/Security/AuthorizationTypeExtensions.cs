// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Extension methods for <see cref="AuthorizationType"/>.
/// </summary>
public static class AuthorizationTypeExtensions
{
    /// <summary>
    /// Converts a string to an <see cref="AuthorizationType"/>.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The corresponding <see cref="AuthorizationType"/>.</returns>
    public static AuthorizationType? ToAuthorizationType(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.ToLowerInvariant() switch
        {
            "none" => AuthorizationType.None,
            "basic" => AuthorizationType.Basic,
            "bearer" => AuthorizationType.Bearer,
            "oauth" => AuthorizationType.OAuth,
            _ => null
        };
    }

    /// <summary>
    /// Converts an <see cref="AuthorizationType"/> to a string.
    /// </summary>
    /// <param name="authorizationType">The authorization type.</param>
    /// <returns>The string representation.</returns>
    public static string? ToTypeString(this AuthorizationType? authorizationType)
    {
        if (!authorizationType.HasValue)
        {
            return null;
        }

        return authorizationType.Value switch
        {
            AuthorizationType.None => "none",
            AuthorizationType.Basic => "basic",
            AuthorizationType.Bearer => "bearer",
            AuthorizationType.OAuth => "oauth",
            _ => null
        };
    }
}
