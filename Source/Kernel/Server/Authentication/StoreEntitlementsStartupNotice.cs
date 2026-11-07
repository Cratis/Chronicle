// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>Describes the configured ordinary-store entitlement mode at startup.</summary>
/// <param name="options">The configuration.</param>
/// <param name="grains">The authoritative grant grain factory.</param>
/// <param name="logger">The logger.</param>
public partial class StoreEntitlementsStartupNotice(IOptions<ChronicleOptions> options, IGrainFactory grains, ILogger<StoreEntitlementsStartupNotice> logger) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var authentication = options.Value.Authentication;
        if (!authentication.Enabled) return;
        if (!authentication.EnforceStoreEntitlements)
        {
            AuditMode(logger);
            return;
        }
        if ((await grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).GetSnapshot()).Grants.Count == 0) EmptyEnforcement(logger);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Event store entitlement audit mode is active. Review EventStoreAccessPreflight and configure explicit grants before enabling EnforceStoreEntitlements. Observations are bounded, best-effort evidence since activation, not policy.")]
    static partial void AuditMode(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement enforcement is active with no explicit grants. Ordinary application access will be denied until grants are configured.")]
    static partial void EmptyEnforcement(ILogger logger);
}
