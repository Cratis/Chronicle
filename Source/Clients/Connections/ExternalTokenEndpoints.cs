// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines the rules for token endpoints and authorities that live outside the Chronicle server.
/// </summary>
internal static class ExternalTokenEndpoints
{
    /// <summary>
    /// Tries to parse a value as an absolute https URI without user info.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="uri">The parsed <see cref="Uri"/> when successful.</param>
    /// <returns>True if the value is acceptable, false otherwise.</returns>
    internal static bool TryParse(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) && IsAcceptable(parsed))
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    /// <summary>
    /// Ensures a <see cref="Uri"/> is an absolute https URI without user info.
    /// </summary>
    /// <param name="uri">The <see cref="Uri"/> to check.</param>
    /// <returns>The same <see cref="Uri"/>.</returns>
    /// <exception cref="InsecureTokenEndpoint">Thrown when the URI is not acceptable.</exception>
    internal static Uri Require(Uri uri)
    {
        if (!IsAcceptable(uri))
        {
            throw new InsecureTokenEndpoint(Describe(uri));
        }

        return uri;
    }

    /// <summary>
    /// Describes a <see cref="Uri"/> for messages and logs: scheme, host, port and path - never user info or query.
    /// </summary>
    /// <param name="uri">The <see cref="Uri"/> to describe.</param>
    /// <returns>The description.</returns>
    internal static string Describe(Uri uri) =>
        uri.IsAbsoluteUri ? $"{uri.Scheme}://{uri.Authority.Substring(uri.Authority.IndexOf('@') + 1)}{uri.AbsolutePath}" : "(relative URI)";

    static bool IsAcceptable(Uri uri) =>
        uri.IsAbsoluteUri && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo);
}
