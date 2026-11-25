// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling projections manager state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="provenance">The database provenance coordinator.</param>
public class ReadModelsManagerStorageProvider(IStorage storage, IReadModelSinkProvenance provenance) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelsManagerState>)!;
        var eventStore = storage.GetEventStore(grainId.Key.ToString()!);
        actualGrainState.State!.ReadModels = await eventStore.ReadModels.GetAll();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelsManagerState>)!;
        var eventStore = storage.GetEventStore(grainId.Key.ToString()!);
        var modified = actualGrainState.State!.Modified.ToArray();
        await ReadModelDefinitionProvenance.Ensure(storage, provenance, grainId.Key.ToString()!, modified);
        foreach (var definition in modified)
        {
            await eventStore.ReadModels.Save(definition);
        }

        actualGrainState.State.Modified = [];
    }
}
