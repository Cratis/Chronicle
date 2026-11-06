// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents authentication configuration for Chronicle.
/// </summary>
public class Authentication
{
    /// <summary>The default for enforcement of explicit event store entitlements.</summary>
    public const bool DefaultEnforceStoreEntitlements = false;

    /// <summary>
    /// The default for <see cref="EnforceAdministratorPolicyForOperations"/>.
    /// </summary>
    public const bool DefaultEnforceAdministratorPolicyForOperations = true;

    /// <summary>
    /// The default for <see cref="EnforceDestructiveOperations"/>.
    /// </summary>
    public const bool DefaultEnforceDestructiveOperations = true;

    /// <summary>
    /// Gets the server-local, single-use administrator recovery request.
    /// </summary>
    public AdministratorRecoveryOptions AdministratorRecovery { get; init; } = new();

    /// <summary>
    /// Gets whether authentication is enforced. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Turning this off removes authentication from the server entirely - no token authority, no identity
    /// endpoints, no bootstrap admin user, and every gRPC service and HTTP endpoint answers anonymously. It
    /// exists for a Chronicle that is not reachable as a server at all: one embedded in a single container or
    /// process, talking to its own client over loopback, disposed with the process. There the credential
    /// exchange protects nothing and costs every client seconds of cold start. Anywhere a network can reach the
    /// server, leaving this off publishes the whole event store to anyone who can open a socket.
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets whether to enforce administrator authorization for System-store operational recovery.
    /// Administrator management is always enforced. When this is off, an operation the administrator policy
    /// would refuse is logged and allowed. Defaults to <see langword="true"/>. Setting it to <see langword="false"/>
    /// is a temporary opt-out that is removed in the next major release.
    /// </summary>
    public bool EnforceAdministratorPolicyForOperations { get; init; } = DefaultEnforceAdministratorPolicyForOperations;

    /// <summary>
    /// Gets whether to refuse non-administrators the destructive operations: compliance key deletion and key re-allowance
    /// that would reach event stores other than the requested one, and the destructive System-store operational
    /// recovery operations. It is independent of <see cref="EnforceAdministratorPolicyForOperations"/> and
    /// <see cref="EnforceStoreEntitlements"/>; either of those being on still enforces its broader scope.
    /// The scope is deliberately narrow: destructive operations on the System store or at kernel scope, and compliance key
    /// fan-out beyond the requested event store. Operations on an ordinary event store, such as an application replaying its
    /// own observers, stay with the per-store rules and are not affected by this flag. Rotating an application secret is
    /// deliberately excluded, because an administrator can rotate it again.
    /// When this is off, such operations are logged and allowed as before. Defaults to <see langword="true"/>.
    /// Setting it to <see langword="false"/> is a temporary opt-out that is removed in the next major release.
    /// </summary>
    public bool EnforceDestructiveOperations { get; init; } = DefaultEnforceDestructiveOperations;

    /// <summary>
    /// Gets whether to enforce explicit event store entitlements. Defaults to false for audit mode.
    /// </summary>
    public bool EnforceStoreEntitlements { get; init; } = DefaultEnforceStoreEntitlements;

    /// <summary>Gets explicit grants for issuer-qualified external principals.</summary>
    public IEnumerable<ExternalEventStoreAccess> EventStoreAccess { get; init; } = [];

    /// <summary>
    /// Gets the authentication authority URL. If not configured, uses the internal OAuth authority.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Gets the opt-in claim that grants administrator privilege to external-authority callers. Null (the default) disables the mapping.
    /// </summary>
    public ExternalAdministratorClaim? ExternalAdministratorClaim { get; init; }

    /// <summary>
    /// Gets the audience external-authority access tokens must target.
    /// </summary>
    public string Audience { get; init; } = "chronicle";

    /// <summary>
    /// Gets a value indicating whether external-authority metadata must use HTTPS.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;

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
    /// Gets the effective administrator username from bootstrap configuration or the default.
    /// </summary>
    public string EffectiveAdminUsername => !string.IsNullOrWhiteSpace(AdminUser?.Username)
        ? AdminUser.Username
        : DefaultAdminUsername;

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

    /// <summary>
    /// Gets an optional operator-provided token for initial administrator setup.
    /// When absent, a token is issued and logged while the administrator has no credentials.
    /// Store this value as a secret and remove it after setup.
    /// </summary>
    public string? InitialAdminSetupToken { get; init; }

    /// <summary>
    /// Gets the per-node password sign-in throttling settings.
    /// </summary>
    public SignInThrottling SignInThrottling { get; init; } = new();

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
