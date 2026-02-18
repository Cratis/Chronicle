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
}
