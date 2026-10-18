// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// The event-store marker and a non-null control record independently impose the floor. A namespace may
    /// initialize in a store whose other namespaces are still legacy. First-time store setup and refused or
    /// awaiting observer initialization do not impose it. This read-only check neither enables nor migrates state.
    /// The caller must run it before using the loaded state or starting any observer work; gate support is a
    /// property of the executing kernel, never inferred from persisted data or supplied by a client.
    /// </remarks>
    public static async Task EnsureObserverCompatibility(this IEventStoreStorage storage, ObserverState state, bool supportsInitializedObservers = false)
    {
        if (!supportsInitializedObservers && (state.TypeSetControl is not null || await storage.AreObserversInitialized()))
        {
            throw new ObserverKernelBelowDowngradeFloor(storage.EventStore, state.Identifier);
        }
    }
}
