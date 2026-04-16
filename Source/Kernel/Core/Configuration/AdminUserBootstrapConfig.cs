// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a bootstrap configuration for the initial admin user.
/// The password is provided as plaintext in config (expected from secrets management)
/// and is hashed internally on load — it is never retained in memory beyond the bootstrap phase.
/// </summary>
public class AdminUserBootstrapConfig
{
    /// <summary>
    /// Gets or inits the admin username.
    /// When empty, falls back to <see cref="Authentication.DefaultAdminUsername"/>.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the admin password in plaintext. This value is hashed immediately on use
    /// and never retained in memory beyond bootstrap.
    /// When empty, the admin user is created without a password and must go through the initial
    /// password setup flow in the Workbench.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the admin user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits a value indicating whether the admin user is required to change their password
    /// on their first login. Defaults to <see langword="false"/>.
    /// When <see langword="true"/>, the user can log in with the configured password but will be
    /// prompted to set a new password before continuing.
    /// </summary>
    public bool RequirePasswordChangeOnFirstLogin { get; init; } = false;
}
