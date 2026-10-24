// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Acquires tokens for managed and workload identity webhook authorization.
/// </summary>
public interface IManagedIdentityTokens
{
    /// <summary>
    /// Acquires a token without canceling other callers sharing its acquisition.
    /// </summary>
    /// <param name="authorization">The identity and scope.</param>
    /// <param name="cancellationToken">Cancels this caller's wait.</param>
    /// <returns>The access token and expiry.</returns>
    Task<AccessTokenInfo> AcquireToken(ManagedIdentityAuthorization authorization, CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates a rejected token without removing a newer replacement.
    /// </summary>
    /// <param name="authorization">The identity and scope.</param>
    /// <param name="token">The rejected token.</param>
    void Invalidate(ManagedIdentityAuthorization authorization, AccessTokenInfo token);
}
