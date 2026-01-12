// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a provider for authentication tokens.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Gets the current access token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token.</returns>
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token by clearing cached tokens and obtaining a new one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new access token.</returns>
    Task<string?> Refresh(CancellationToken cancellationToken = default);
}
