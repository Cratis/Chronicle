// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Types;

// Primary-constructor parameters used inside #if DEVELOPMENT trip CS9113 in release builds.
#pragma warning disable CS9113

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Wipes the kernel back to a freshly bootstrapped state.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> instance.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> instance.</param>
/// <param name="resetHandlers">Storage components that wipe their backing store during a development reset.</param>
/// <param name="bootstrapResetHandler">Re-runs kernel bootstrap (identity, system event store) after storage is wiped.</param>
/// <remarks>
/// This exists for integration tests, which need a known-empty server between test classes. It is compiled in only
/// when the server is built with the DEVELOPMENT preprocessor symbol - a released kernel cannot be asked to do this.
/// </remarks>
internal sealed class KernelStateResetter(
    IGrainFactory grainFactory,
    IProjectionPipelineManager projectionPipelineManager,
    IInstancesOf<ICanPerformKernelStateReset> resetHandlers,
    KernelBootstrapResetHandler bootstrapResetHandler) : IKernelStateResetter
{
    /// <summary>
    /// Gets a value indicating whether the server exposes development tools.
    /// </summary>
    internal static bool IsAvailable =>
#if DEVELOPMENT
        true;
#else
        false;
#endif

    /// <summary>
    /// Resets the kernel state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DevelopmentToolsNotAvailable">Thrown when the server was not built with development tools.</exception>
    public async Task Reset()
    {
#if DEVELOPMENT
        await KeepAliveGrainReset.Reset(grainFactory);
        projectionPipelineManager.Clear();

        // Wipe storage BEFORE deactivating grains, as the in-process test fixture does. Deactivating
        // first let a reminder re-activate a grain between deactivation and the wipe - notably
        // EventStoreSubscriptionsManager, whose subscription reminder first fires after 100 ms - with its in-memory
        // state loaded from the previous test class. That state survived the wipe and was persisted
        // again on the grain's next write, so a test that subscribed once found the previous test's
        // subscription stored next to its own (#5336). Wiping first also truncates the reminders the
        // storage holds, and any grain writing during deactivation writes into an empty store.
        await WipeStorage();
        await DeactivateAllGrains();

        // Deactivating grains write their dirty state in OnDeactivateAsync, so wipe again to remove
        // anything written back between the first wipe and the end of the deactivation cycle.
        await WipeStorage();
        projectionPipelineManager.Clear();

        // Re-run kernel bootstrap once storage is empty. The startup task only runs once per
        // silo lifetime, so without this the next test class would hit an empty identity DB
        // and every gRPC call would fail with 401.
        await bootstrapResetHandler.Bootstrap();
#else
        await Task.CompletedTask;
        throw new DevelopmentToolsNotAvailable();
#endif
    }

#if DEVELOPMENT
    async Task WipeStorage()
    {
        // IInstancesOf may include backend-specific handlers whose dependencies are not
        // registered in the active storage mode (e.g. MongoDBKernelStateResetHandler when
        // running on SQL storage). Those handlers cannot be constructed by DI and surface
        // as null entries in the enumeration; skip them.
        foreach (var handler in resetHandlers)
        {
            if (handler?.CanReset() != true)
            {
                continue;
            }

            await handler.Reset();
        }
    }

    async Task DeactivateAllGrains()
    {
        var managementGrain = grainFactory.GetGrain<IManagementGrain>(0);
        await managementGrain.ForceActivationCollection(TimeSpan.Zero);

        // ForceActivationCollection returns once the broadcast is acknowledged, not when every
        // grain has finished deactivating. Wait until the activation count holds steady instead
        // of guessing how long deactivation takes. The second wipe runs either way, so a count
        // that never settles within the bound only means the reset proceeds without waiting longer.
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stableReadCount = 0;
        var previousCount = -1;
        while (stableReadCount < 3 && !cancellationTokenSource.IsCancellationRequested)
        {
            var currentCount = await managementGrain.GetTotalActivationCount();
            stableReadCount = currentCount == previousCount ? stableReadCount + 1 : 0;
            previousCount = currentCount;
            await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
#endif
}
