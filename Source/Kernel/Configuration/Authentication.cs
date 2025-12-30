// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents authentication configuration for Chronicle.
/// </summary>
public class Authentication
{
    /// <summary>
    /// Gets whether authentication is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true; // Default to enabled

    /// <summary>
    /// Gets the authentication authority URL. If not configured, uses the internal OAuth authority.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Gets whether to use the internal OAuth authority.
    /// </summary>
    public bool UseInternalAuthority { get; init; } = true;

    /// <summary>
    /// Gets the default admin username.
    /// </summary>
    public string DefaultAdminUsername { get; init; } = "admin";

    /// <summary>
    /// Gets the default admin password.
    /// </summary>
    public string? DefaultAdminPassword { get; init; }
}
