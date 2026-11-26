// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Identifies the endpoint, client and optional discovery authority a cached token belongs to.
/// </summary>
/// <param name="TokenEndpoint">The exact token endpoint.</param>
/// <param name="ClientId">The OAuth client id.</param>
/// <param name="Authority">The discovery authority, when applicable.</param>
internal sealed record TokenCacheBinding(string TokenEndpoint, string ClientId, string? Authority = null)
{
    /// <summary>
    /// Gets whether the binding contains both required identities.
    /// </summary>
    /// <returns>Whether the binding can be verified.</returns>
    internal bool IsKnown() => !string.IsNullOrWhiteSpace(TokenEndpoint) && !string.IsNullOrWhiteSpace(ClientId);
}
