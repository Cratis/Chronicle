// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling reducer state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerDefinitionStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ReducerDefinitionStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var reducerKey = ReducerKey.Parse(grainId.Key.ToString());
        var eventStore = storage.GetEventStore(reducerKey.EventStore);
        await ReadModelProvenance.For(storage, reducerKey.EventStore).Preserve();
        await eventStore.Reducers.Delete(reducerKey.ReducerId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReducerDefinition>)!;
        var reducerKey = ReducerKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(reducerKey.EventStore);

        if (!await eventStore.Reducers.Has(reducerKey.ReducerId)) return;
        actualGrainState.State = await eventStore.Reducers.Get(reducerKey.ReducerId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReducerDefinition>)!;
        var reducerKey = ReducerKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(reducerKey.EventStore);
        await ReadModelProducerWriteGuard.Ensure(storage, reducerKey.EventStore, actualGrainState.State!.EventSequenceId);
        await eventStore.Reducers.Save(actualGrainState.State!);
    }
}
