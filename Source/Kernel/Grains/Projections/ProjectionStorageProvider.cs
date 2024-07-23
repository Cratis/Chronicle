// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling projection state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ProjectionStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        return eventStore.Projections.Delete(projectionKey.ProjectionId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionDefinition>)!;
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);

        if (!await eventStore.Projections.Has(projectionKey.ProjectionId)) return;
        actualGrainState.State = await eventStore.Projections.Get(projectionKey.ProjectionId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionDefinition>)!;
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        await eventStore.Projections.Save(actualGrainState.State);
    }
}
