// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ObserverStateGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Observers;
        actualGrainState.State = await observers.Get(observerKey.ObserverId);
        actualGrainState.State = actualGrainState.State with
        {
            Identifier = observerKey.ObserverId
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Observers;
        await observers.Save(actualGrainState.State);
    }
}
