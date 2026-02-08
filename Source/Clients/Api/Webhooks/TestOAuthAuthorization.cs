// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents a command to test OAuth authorization.
/// </summary>
public class TestOAuthAuthorization
{
    /// <summary>
    /// Gets or sets the OAuth authority URL.
    /// </summary>
    public string OAuthAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    public string OAuthClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string OAuthClientSecret { get; set; } = string.Empty;
}
