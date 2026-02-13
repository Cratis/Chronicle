// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Observation.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling reactor state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ReactorDefinitionStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var reactorKey = ReactorKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(reactorKey.EventStore);
        return eventStore.Reactors.Delete(reactorKey.ReactorId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReactorDefinition>)!;
        var reactorKey = ReactorKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(reactorKey.EventStore);

        if (!await eventStore.Reactors.Has(reactorKey.ReactorId)) return;
        actualGrainState.State = await eventStore.Reactors.Get(reactorKey.ReactorId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReactorDefinition>)!;
        var reactorKey = ReactorKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(reactorKey.EventStore);
        await eventStore.Reactors.Save(actualGrainState.State);
    }
}
