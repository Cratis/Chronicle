// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>Describes the configured ordinary-store entitlement mode once the silo is up.</summary>
/// <param name="options">The configuration.</param>
/// <param name="entitlements">The local silo's <see cref="IStoreEntitlementsCache"/>, loaded by the startup task.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// The grants are read from the entitlement cache the startup task has already loaded, not through a grain call. A
/// hosted service starts before the listeners bind and before the deferred silo starts, so a grain call there holds
/// the bind behind a silo that cannot start until it returns (#4040); and a grain call from the silo lifecycle would
/// hold readiness behind a busy silo for up to a response timeout.
/// </remarks>
public class StoreEntitlementsStartupNotice(IOptions<ChronicleOptions> options, IStoreEntitlementsCache entitlements, ILogger<StoreEntitlementsStartupNotice> logger) : ILifecycleParticipant<ISiloLifecycle>
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
    /// The notice is advice for an operator; grants that could not be loaded are logged and do not fail the silo start.
    /// </remarks>
    public Task Notify(CancellationToken cancellationToken)
    {
        var authentication = options.Value.Authentication;
        if (!authentication.Enabled) return Task.CompletedTask;
        if (!authentication.EnforceStoreEntitlements)
        {
            logger.AuditMode();
            return Task.CompletedTask;
        }

        if (!entitlements.IsFresh)
        {
            logger.GrantsUnavailable();
        }
        else if (entitlements.Snapshot.GrantCount == 0)
        {
            logger.EmptyEnforcement();
        }

        return Task.CompletedTask;
    }
}
