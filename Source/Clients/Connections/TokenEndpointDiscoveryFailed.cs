// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when the token endpoint cannot be discovered from the configured authority.
/// </summary>
/// <remarks>
/// The client never falls back to the Chronicle server's token endpoint once an authority is configured.
/// </remarks>
/// <param name="authority">A description of the authority that carries no user info or query.</param>
/// <param name="innerException">The underlying failure.</param>
public class TokenEndpointDiscoveryFailed(string authority, Exception innerException) :
    Exception($"Could not discover the token endpoint from the configured Authentication:Authority '{authority}'. Fix the authority, or remove it to request tokens from the Chronicle server.", innerException);
