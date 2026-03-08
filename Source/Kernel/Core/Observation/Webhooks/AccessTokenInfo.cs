// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents information about an OAuth access token.
/// </summary>
/// <param name="AccessToken">The access token.</param>
/// <param name="ExpiresAt">When the token expires.</param>
public record AccessTokenInfo(string AccessToken, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a value indicating whether the token is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Gets an empty AccessTokenInfo representing no token.
    /// </summary>
    public static AccessTokenInfo Empty => new(string.Empty, DateTimeOffset.MinValue);
}
