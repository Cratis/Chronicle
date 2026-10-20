// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Defines a system that handles OAuth authentication for webhooks.
/// </summary>
public interface IOAuthClient
{
    /// <summary>
    /// Acquires an access token using client credentials flow.
    /// </summary>
    /// <param name="authorization">The OAuth authorization configuration.</param>
    /// <returns>A <see cref="Task{T}"/> with the access token information.</returns>
    Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization);

    /// <summary>
    /// Acquires a token without propagating caller cancellation to other waiters.
    /// </summary>
    /// <param name="authorization">The OAuth configuration.</param>
    /// <param name="cancellationToken">Cancels only this caller's wait.</param>
    /// <returns>The token information.</returns>
    Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, CancellationToken cancellationToken) =>
        AcquireToken(authorization).WaitAsync(cancellationToken);

    /// <summary>
    /// Invalidates a rejected token without removing a newer replacement.
    /// </summary>
    /// <param name="authorization">The OAuth configuration.</param>
    /// <param name="token">The rejected token.</param>
    void Invalidate(OAuthAuthorization authorization, AccessTokenInfo token)
    {
    }
}
