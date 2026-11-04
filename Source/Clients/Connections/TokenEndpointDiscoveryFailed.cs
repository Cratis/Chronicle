// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when the token endpoint cannot be discovered from the configured authority.
/// </summary>
/// <remarks>
/// The client never falls back to the Chronicle server's token endpoint once an authority is configured.
/// The message carries only a fixed failure label; the authority and the underlying failure are never retained.
/// </remarks>
public class TokenEndpointDiscoveryFailed : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenEndpointDiscoveryFailed"/> class.
    /// </summary>
    /// <param name="authority">Ignored - the authority is never included in the message.</param>
    /// <param name="innerException">The underlying failure - only its category is kept, never the exception itself.</param>
    public TokenEndpointDiscoveryFailed(string authority, Exception innerException)
        : this(TokenDiagnostics.Categorize(innerException))
    {
        _ = authority;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenEndpointDiscoveryFailed"/> class.
    /// </summary>
    /// <param name="category">The code-owned failure category.</param>
    internal TokenEndpointDiscoveryFailed(TokenFailureCategory category)
        : base($"Could not discover the token endpoint from the configured Authentication:Authority (reason: {TokenDiagnostics.Label(category)}). Fix the authority, or remove it to request tokens from the Chronicle server.")
    {
    }
}
