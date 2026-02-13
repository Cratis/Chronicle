// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling read model state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReadModelDefinitionStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ReadModelDefinitionStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var readModelKey = ReadModelGrainKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(readModelKey.EventStore);
        return eventStore.ReadModels.Delete(readModelKey.Identifier);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelDefinition>)!;
        var readModelKey = ReadModelGrainKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(readModelKey.EventStore);

        if (!await eventStore.ReadModels.Has(readModelKey.Identifier)) return;
        actualGrainState.State = await eventStore.ReadModels.Get(readModelKey.Identifier);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelDefinition>)!;
        var readModelKey = ReadModelGrainKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(readModelKey.EventStore);
        await eventStore.ReadModels.Save(actualGrainState.State);
    }
}
