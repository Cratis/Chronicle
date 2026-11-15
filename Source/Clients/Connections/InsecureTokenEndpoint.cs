// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a token endpoint or authority is not an absolute https URL without user info.
/// </summary>
/// <remarks>
/// The message carries only a fixed source label, never any part of the offending URL.
/// </remarks>
public class InsecureTokenEndpoint : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsecureTokenEndpoint"/> class.
    /// </summary>
    /// <param name="description">Ignored - no part of the offending URL is included in the message.</param>
    public InsecureTokenEndpoint(string description)
        : this(TokenEndpointSource.Configured)
    {
        _ = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsecureTokenEndpoint"/> class.
    /// </summary>
    /// <param name="source">Where the offending endpoint came from.</param>
    internal InsecureTokenEndpoint(TokenEndpointSource source)
        : base($"The {TokenDiagnostics.Label(source)} token endpoint or authority must be an absolute https URL without user info.")
    {
    }
}
