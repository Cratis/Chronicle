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
        if (!authentication.Enabled) WarnAuthenticationDisabled(logger);
        else if (!authentication.EnforceAdministratorPolicyForOperations) WarnOperationsNotEnforced(logger);
        if (authentication.ExternalAdministratorClaim is { } mapping)
        {
            if (!authentication.Enabled) WarnMappingIgnored(logger);
            else if (!authentication.UseInternalAuthority) LogMappingEnabled(logger, mapping.Type);
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

    [LoggerMessage(LogLevel.Warning, "Administrator policy for operations is not enforced; operations it would refuse are logged and allowed. Set Authentication:EnforceAdministratorPolicyForOperations to true to enforce it")]
    static partial void WarnOperationsNotEnforced(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "External-authority administrator mapping is configured but authentication is disabled; it has no effect")]
    static partial void WarnMappingIgnored(ILogger logger);

    [LoggerMessage(LogLevel.Information, "External-authority administrator mapping is enabled for claim type {ClaimType}")]
    static partial void LogMappingEnabled(ILogger logger, string? claimType);
}
