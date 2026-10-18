// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Checks the downgrade floor of decision 0011 before an observer is activated.
/// </summary>
public static class ObserverStorageCompatibility
{
    /// <summary>
    /// Refuses initialized observer storage on a kernel that does not implement the gate release's contract.
    /// </summary>
    /// <param name="storage">The observer's event-store storage.</param>
    /// <param name="state">The namespace observer state just read from storage.</param>
    /// <param name="supportsInitializedObservers">Whether this kernel implements the gate release, including owner admission. Defaults to false for dark state-slice releases.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ObserverKernelBelowDowngradeFloor">Initialized storage requires the gate release.</exception>
    /// <remarks>
    /// The event-store marker, a non-null control record and initializing or initialized observer state independently impose the floor. A namespace may
    /// initialize in a store whose other namespaces are still legacy. First-time store setup and refused or
    /// awaiting observer initialization do not impose it. This read-only check neither enables nor migrates state.
    /// The caller must run it before using the loaded state or starting any observer work; gate support is a
    /// property of the executing kernel, never inferred from persisted data or supplied by a client.
    /// </remarks>
    public static Task EnsureObserverCompatibility(this IEventStoreStorage storage, ObserverState state, bool supportsInitializedObservers = false) =>
        storage.EnsureObserverCompatibility(state.Identifier, state, supportsInitializedObservers);

    /// <summary>
    /// Refuses observer activation using the requested identity even when no observer record exists.
    /// </summary>
    /// <param name="storage">The observer's event-store storage.</param>
    /// <param name="observerId">The observer identity from the activation key.</param>
    /// <param name="state">The namespace observer state just read from storage.</param>
    /// <param name="supportsInitializedObservers">Whether this kernel implements the gate release, including owner admission.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ObserverKernelBelowDowngradeFloor">Initialized storage requires the gate release.</exception>
    public static async Task EnsureObserverCompatibility(this IEventStoreStorage storage, ObserverId observerId, ObserverState state, bool supportsInitializedObservers = false)
    {
        if (!supportsInitializedObservers &&
            (state.TypeSetControl is not null ||
             state.Initialization?.State is ObserverInitializationState.Initializing or ObserverInitializationState.Initialized ||
             await storage.AreObserversInitialized()))
        {
            throw new ObserverKernelBelowDowngradeFloor(storage.EventStore, observerId);
        }
    }
}
