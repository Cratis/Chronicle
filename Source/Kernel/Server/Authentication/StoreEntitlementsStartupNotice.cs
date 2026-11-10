// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>Describes the configured ordinary-store entitlement mode once the silo is up.</summary>
/// <param name="options">The configuration.</param>
/// <param name="grains">The authoritative grant grain factory.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// Reading the grants is a grain call, so it belongs to the silo's lifecycle rather than to a hosted service. A hosted
/// service starts before the listeners bind and before the deferred silo starts, so a grain call there holds the bind
/// behind a silo that cannot start until it returns (#4040).
/// </remarks>
public partial class StoreEntitlementsStartupNotice(IOptions<ChronicleOptions> options, IGrainFactory grains, ILogger<StoreEntitlementsStartupNotice> logger) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <summary>
    /// The silo lifecycle stage the notice is given at: after the startup task has made the silo active.
    /// </summary>
    internal const int Stage = ServiceLifecycleStage.Active + 1;

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(StoreEntitlementsStartupNotice), Stage, Notify);

    /// <summary>
    /// Logs the notice for the configured entitlement mode.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the silo start.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The notice is advice for an operator; failing to read the grants is logged and does not fail the silo start.
    /// </remarks>
    public async Task Notify(CancellationToken cancellationToken)
    {
        var authentication = options.Value.Authentication;
        if (!authentication.Enabled) return;
        if (!authentication.EnforceStoreEntitlements)
        {
            AuditMode(logger);
            return;
        }

        try
        {
            var snapshot = await grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).GetSnapshot().WaitAsync(cancellationToken);
            if (snapshot.Grants.Count == 0) EmptyEnforcement(logger);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            GrantsUnavailable(logger, exception);
        }
    }

    [LoggerMessage(LogLevel.Information, "Event store entitlement audit mode is active. Review EventStoreAccessPreflight and configure explicit grants before enabling EnforceStoreEntitlements. Observations are bounded, best-effort evidence since activation, not policy.")]
    static partial void AuditMode(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement enforcement is active with no explicit grants. Ordinary application access will be denied until grants are configured.")]
    static partial void EmptyEnforcement(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement enforcement is active, but the configured grants could not be read to check that any exist.")]
    static partial void GrantsUnavailable(ILogger logger, Exception exception);
}
