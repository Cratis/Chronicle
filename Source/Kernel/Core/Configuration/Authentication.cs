// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents authentication configuration for Chronicle.
/// </summary>
public class Authentication
{
    /// <summary>
    /// Gets the authentication authority URL. If not configured, uses the internal OAuth authority.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Gets whether to use the internal OAuth authority. Returns true when Authority is not set.
    /// </summary>
    public bool UseInternalAuthority => string.IsNullOrEmpty(Authority);

    /// <summary>
    /// Gets the default admin username used when <see cref="AdminUser"/> is not configured
    /// or its <see cref="AdminUserBootstrapConfig.Username"/> is empty.
    /// </summary>
    public string DefaultAdminUsername { get; init; } = "admin";

    /// <summary>
    /// Gets the bootstrap configuration for the initial admin user.
    /// When set and <see cref="AdminUserBootstrapConfig.Password"/> is non-empty, Chronicle will
    /// create the admin user with the given credentials on first startup. The password is hashed
    /// immediately on use and never retained in memory.
    /// When not set (or password is empty), Chronicle falls back to the default behavior:
    /// the admin user is created without a password and must go through the initial password
    /// setup flow in the Workbench.
    /// </summary>
    public AdminUserBootstrapConfig? AdminUser { get; init; }

#if DEVELOPMENT
    /// <summary>
    /// Gets the default admin password for development. When set, the admin user is created with this password pre-configured,
    /// bypassing the initial password setup flow. Only available in development builds.
    /// The password is read from configuration in plain text — use only in isolated development environments,
    /// never in staging or production.
    /// </summary>
    /// <remarks>
    /// Prefer using <see cref="AdminUser"/> with a configured password instead — it works in all environments
    /// and integrates with secrets management solutions.
    /// </remarks>
    public string DefaultAdminPassword { get; init; } = string.Empty;
#endif
}
