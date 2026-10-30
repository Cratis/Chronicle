// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the default provider for hosts without managed identity support.
/// </summary>
public class ManagedIdentityTokens : IManagedIdentityTokens
{
    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(ManagedIdentityAuthorization authorization, CancellationToken cancellationToken) =>
        Task.FromException<AccessTokenInfo>(new ManagedIdentityNotAvailable());

    /// <inheritdoc/>
    public void Invalidate(ManagedIdentityAuthorization authorization, AccessTokenInfo token)
    {
        // This provider never acquires or caches tokens.
    }
}
