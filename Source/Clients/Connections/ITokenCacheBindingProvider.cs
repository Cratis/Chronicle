// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the identities used to verify persisted tokens without exposing credentials.
/// </summary>
internal interface ITokenCacheBindingProvider
{
    /// <summary>
    /// Gets a token and its binding captured together under the provider's token lock.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An immutable snapshot, or null when no token is available.</returns>
    Task<TokenWithBinding?> GetTokenWithBinding(CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes a token and captures its binding under the provider's token lock.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An immutable snapshot, or null when no token is available.</returns>
    Task<TokenWithBinding?> RefreshTokenWithBinding(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the binding currently expected by the provider, discovering the endpoint if necessary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current binding, or null when it cannot be verified.</returns>
    ValueTask<TokenCacheBinding?> GetCurrentCacheBinding(CancellationToken cancellationToken);
}
