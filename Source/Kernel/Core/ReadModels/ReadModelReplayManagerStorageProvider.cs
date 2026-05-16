// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling read model replay manager state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ReadModelReplayManagerStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelReplayManagerState>)!;
        var key = ReadModelReplayManagerGrainKey.Parse(grainId.Key.ToString()!);
        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);

        actualGrainState.State.Occurrences = (await namespaceStorage.ReplayedReadModels.GetOccurrences(key.ReadModel)).ToList();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ReadModelReplayManagerState>)!;

        var key = ReadModelReplayManagerGrainKey.Parse(grainId.Key.ToString()!);
        var eventStoreStorage = storage.GetEventStore(key.EventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(key.Namespace);

        foreach (var occurrence in actualGrainState.State.NewOccurrences)
        {
            await namespaceStorage.ReplayedReadModels.Replayed(occurrence);
        }

        if (actualGrainState.State.RemovedOccurrences.Count > 0)
        {
            var readModelDefinition = await eventStoreStorage.ReadModels.Get(key.ReadModel);
            var sink = await namespaceStorage.Sinks.GetFor(readModelDefinition);
            foreach (var occurrence in actualGrainState.State.RemovedOccurrences)
            {
                await sink.Remove(occurrence.RevertContainerName);
                await namespaceStorage.ReplayedReadModels.Remove(occurrence);
            }
        }

        actualGrainState.State.NewOccurrences.Clear();
        actualGrainState.State.RemovedOccurrences.Clear();
    }
}
