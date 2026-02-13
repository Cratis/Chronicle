// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.ReadModels;

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
        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);

        foreach (var occurrence in actualGrainState.State.NewOccurrences)
        {
            await namespaceStorage.ReplayedReadModels.Replayed(occurrence);
        }

        actualGrainState.State.NewOccurrences.Clear();
    }
}
