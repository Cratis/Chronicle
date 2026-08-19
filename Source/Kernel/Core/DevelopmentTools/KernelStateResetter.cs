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
    KernelBootstrapResetHandler bootstrapResetHandler)
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
    internal async Task Reset()
    {
#if DEVELOPMENT
        var managementGrain = grainFactory.GetGrain<IManagementGrain>(0);
        await managementGrain.ForceActivationCollection(TimeSpan.Zero);

        // ForceActivationCollection returns once the broadcast is acknowledged, not when every
        // grain has actually finished deactivating. Deactivating grains write their state on
        // OnDeactivateAsync, so if we proceed straight to storage truncation the soon-to-die
        // grains can re-persist stale state (e.g. EventSequence.SequenceNumber) right back
        // into the table we just emptied. Give them a brief window to flush.
        await Task.Delay(500);

        projectionPipelineManager.Clear();

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

        // Re-run kernel bootstrap once storage is empty. The startup task only runs once per
        // silo lifetime, so without this the next test class would hit an empty identity DB
        // and every gRPC call would fail with 401.
        await bootstrapResetHandler.Bootstrap();
#else
        await Task.CompletedTask;
        throw new DevelopmentToolsNotAvailable();
#endif
    }
}
