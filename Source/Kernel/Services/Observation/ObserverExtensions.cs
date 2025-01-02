// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Helper methods for working with observers.
/// </summary>
public static class ObserverExtensions
{
    /// <summary>
    /// Get the observer for a specific command.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    /// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
    /// <param name="command"><see cref="IObserverCommand"/> to get the observer for.</param>
    /// <returns>An instance of the observer.</returns>
    public static IObserver GetObserver(this IGrainFactory grainFactory, IStorage storage, IObserverCommand command)
    {
        var observerState = storage.GetEventStore(command.EventStore).GetNamespace(command.Namespace).Observers.Get(command.ObserverId).Result;
        var key = new ObserverKey(command.ObserverId, command.EventStore, command.Namespace, observerState.EventSequenceId);
        return grainFactory.GetGrain<IObserver>(key);
    }
}
