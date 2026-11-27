// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
#if DEVELOPMENT
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
#endif

// Primary-constructor parameters used inside #if DEVELOPMENT trip CS9113 in release builds.
#pragma warning disable CS9113

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Wipes the kernel back to a freshly bootstrapped state.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> instance.</param>
/// <param name="storage">The storage inventory of event stores whose subscriptions must be stopped.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> instance.</param>
/// <param name="resetHandlers">Storage components that wipe their backing store during a development reset.</param>
/// <param name="eventStoreInitialization">The event store initialization cache to clear.</param>
/// <param name="bootstrapResetHandler">Re-runs kernel bootstrap (identity, system event store) after storage is wiped.</param>
/// <param name="provenanceCache">The active silo observation cache.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// This exists for integration tests, which need a known-empty server between test classes. It is compiled in only
/// when the server is built with the DEVELOPMENT preprocessor symbol - a released kernel cannot be asked to do this.
/// </remarks>
#if DEVELOPMENT
internal sealed partial class KernelStateResetter(
#else
internal sealed class KernelStateResetter(
#endif
    IGrainFactory grainFactory,
    IStorage storage,
    IProjectionPipelineManager projectionPipelineManager,
    IEnumerable<ICanPerformKernelStateReset> resetHandlers,
    IEventStoreInitialization eventStoreInitialization,
    KernelBootstrapResetHandler bootstrapResetHandler,
    ISinkProvenanceCache provenanceCache,
    ILogger<KernelStateResetter> logger) : IKernelStateResetter
{
#if DEVELOPMENT
    readonly SemaphoreSlim _resetGate = new(1, 1);
#endif

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
        await _resetGate.WaitAsync();
        try
        {
            await ResetCore();
        }
        finally
        {
            _resetGate.Release();
        }
#else
        await Task.CompletedTask;
        throw new DevelopmentToolsNotAvailable();
#endif
    }

#if DEVELOPMENT
    async Task ResetCore()
    {
        var observations = provenanceCache as IDevelopmentSinkProvenanceObservations
            ?? throw new InvalidOperationException("The active provenance cache does not support development reset.");
        await KeepAliveGrainReset.Reset(grainFactory);
        await EventStoreSubscriptionsReset.Reset(grainFactory, storage);

        // Removing subscriptions can activate source observers to unsubscribe them. Quiesce those too.
        await KeepAliveGrainReset.Reset(grainFactory);
        projectionPipelineManager.Clear();

        // Wipe persisted reminders before deactivation so they cannot reactivate owners with stale state (#5336).
        await WipeStorage();
        await DeactivateAllGrains();

        // Deactivating grains may write dirty state back, so wipe again after activation collection.
        await WipeStorage();

        // Cached stores bypass catalog creation, but the wipe removed their persisted catalog entries.
        storage.Clear();
        await observations.RotateAfterWipe();
        eventStoreInitialization.Clear();
        projectionPipelineManager.Clear();

        // Re-run kernel bootstrap once storage is empty. The startup task only runs once per
        // silo lifetime, so without this the next test class would hit an empty identity DB
        // and every gRPC call would fail with 401.
        await bootstrapResetHandler.Bootstrap();
    }

    [LoggerMessage(LogLevel.Warning, "Kernel grain activations did not settle before the development reset continued.")]
    static partial void LogDeactivationTimeout(ILogger logger);

    async Task WipeStorage()
    {
        // Storage builders register only their active backend's reset handler. Discovering every
        // implementation would try to construct inactive handlers before CanReset can skip them.
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

        // ForceActivationCollection acknowledges the broadcast before every grain has deactivated.
        // Wait for a stable count, but do not let unrelated busy grains prevent the second wipe.
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

        if (cancellationTokenSource.IsCancellationRequested)
        {
            LogDeactivationTimeout(logger);
        }
    }
#endif
}
