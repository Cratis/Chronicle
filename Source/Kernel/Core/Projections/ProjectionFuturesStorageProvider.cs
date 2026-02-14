// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling projection futures storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ProjectionFuturesStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionFuturesState>)!;
        var projectionKey = ProjectionFuturesKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        var futuresStorage = eventStore.GetNamespace(projectionKey.Namespace).ProjectionFutures;
        await futuresStorage.RemoveAllForProjection(projectionKey.ProjectionId);
        actualGrainState.State = ProjectionFuturesState.Empty;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionFuturesState>)!;
        var projectionKey = ProjectionFuturesKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        var futuresStorage = eventStore.GetNamespace(projectionKey.Namespace).ProjectionFutures;
        var futures = await futuresStorage.GetForProjection(projectionKey.ProjectionId);
        actualGrainState.State = new()
        {
            Futures = futures.ToList()
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionFuturesState>)!;
        var projectionKey = ProjectionFuturesKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        var futuresStorage = eventStore.GetNamespace(projectionKey.Namespace).ProjectionFutures;
        foreach (var future in actualGrainState.State.AddedFutures)
        {
            await futuresStorage.Save(projectionKey.ProjectionId, future);
        }
        foreach (var future in actualGrainState.State.ResolvedFutures)
        {
            await futuresStorage.Remove(projectionKey.ProjectionId, future.Id);
        }
        actualGrainState.State.AddedFutures.Clear();
        actualGrainState.State.ResolvedFutures.Clear();
    }
}
