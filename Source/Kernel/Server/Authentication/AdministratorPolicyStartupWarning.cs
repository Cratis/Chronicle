// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Warns once at startup when administrator protection is disabled with authentication.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
public partial class AdministratorPolicyStartupWarning(IOptions<ChronicleOptions> options, ILogger<AdministratorPolicyStartupWarning> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var authentication = options.Value.Authentication;
        if (!authentication.Enabled)
        {
            WarnAuthenticationDisabled(logger);
        }
        else
        {
            if (!authentication.EnforceAdministratorPolicyForOperations) WarnOperationsNotEnforced(logger);
            if (!authentication.EnforceDestructiveOperations) WarnDestructiveOperationsNotEnforced(logger);
        }

        if (authentication.ExternalAdministratorClaim is { } mapping)
        {
            if (!authentication.Enabled) WarnMappingIgnored(logger);
            else if (!authentication.UseInternalAuthority) LogMappingEnabled(logger, mapping.Type);
        }

        if (authentication.Http.IsTrustedProxy)
        {
            if (!authentication.Enabled) WarnTrustedProxyIgnored(logger);
            else if (string.IsNullOrWhiteSpace(authentication.Http.TrustedProxy.AdministratorGroup)) WarnEveryProxyUserIsAdministrator(logger);
            else LogTrustedProxyAdministratorGroup(logger, authentication.Http.TrustedProxy.AdministratorGroup);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Logs the authentication-off warning once per server startup.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(LogLevel.Warning, "Authentication is disabled; administrator authorization allows all callers")]
    static partial void WarnAuthenticationDisabled(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Administrator policy for operations is not enforced; operations it would refuse are logged and allowed. Authentication:EnforceAdministratorPolicyForOperations defaults to false; the default becomes enforcing in the next major release. Set it to true to opt in now")]
    static partial void WarnOperationsNotEnforced(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Destructive operations are not enforced; compliance key operations beyond the requested event store are logged and allowed. Authentication:EnforceDestructiveOperations defaults to false; the default becomes enforcing in the next major release. Set it to true to opt in now")]
    static partial void WarnDestructiveOperationsNotEnforced(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "External-authority administrator mapping is configured but authentication is disabled; it has no effect")]
    static partial void WarnMappingIgnored(ILogger logger);

    [LoggerMessage(LogLevel.Information, "External-authority administrator mapping is enabled for claim type {ClaimType}")]
    static partial void LogMappingEnabled(ILogger logger, string? claimType);

    [LoggerMessage(LogLevel.Warning, "Authentication:Http:Mode is TrustedProxy but authentication is disabled; it has no effect")]
    static partial void WarnTrustedProxyIgnored(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "HTTP callers are authenticated by a trusted proxy and Authentication:Http:TrustedProxy:AdministratorGroup is not set; every user the proxy authenticates is a Chronicle administrator")]
    static partial void WarnEveryProxyUserIsAdministrator(ILogger logger);

    [LoggerMessage(LogLevel.Information, "HTTP callers are authenticated by a trusted proxy; members of group {Group} are Chronicle administrators")]
    static partial void LogTrustedProxyAdministratorGroup(ILogger logger, string? group);
}
