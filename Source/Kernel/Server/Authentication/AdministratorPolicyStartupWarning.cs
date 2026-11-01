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
        if (!options.Value.Authentication.Enabled) WarnAuthenticationDisabled(logger);
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
}
