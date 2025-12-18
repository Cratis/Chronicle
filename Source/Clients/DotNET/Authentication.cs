// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the authentication configuration.
/// </summary>
public class Authentication
{
    /// <summary>
    /// Gets or sets the authentication mode.
    /// </summary>
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.None;

    /// <summary>
    /// Gets or sets whether authentication is enabled.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets whether to use the internal OAuth authority.
    /// </summary>
    public bool UseInternalAuthority { get; set; } = true;

    /// <summary>
    /// Gets or sets the default admin username.
    /// </summary>
    public string Username { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the default admin password.
    /// </summary>
    public string Password { get; set; } = "ChangeMeNow!";

    /// <summary>
    /// Gets or sets the API key for API key authentication.
    /// </summary>
    public string ApiKey { get; set; } = "ChangeMeNow!";
}
