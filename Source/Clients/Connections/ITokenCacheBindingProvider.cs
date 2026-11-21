// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the identities used to verify persisted tokens without exposing credentials.
/// </summary>
internal interface ITokenCacheBindingProvider
{
    /// <summary>
    /// Gets the binding of the last successfully acquired token, not a newly selected endpoint.
    /// </summary>
    TokenCacheBinding? AcquiredTokenBinding { get; }

    /// <summary>
    /// Gets the binding currently expected by the provider, discovering the endpoint if necessary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current binding, or null when it cannot be verified.</returns>
    ValueTask<TokenCacheBinding?> GetCurrentCacheBinding(CancellationToken cancellationToken);
}
