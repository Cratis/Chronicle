// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ObserverGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Observers;
        actualGrainState.State = await observers.GetState(observerKey.ObserverId, observerKey);
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).Observers;
        await observers.SaveState(observerKey.ObserverId, observerKey, actualGrainState.State);
    }
}
