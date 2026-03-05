// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the storage provider for constraints.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to use.</param>
public class ConstraintsStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ConstraintsState>)!;

        var key = (ConstraintsKey)grainId.Key.ToString()!;
        var eventStore = storage.GetEventStore(key.EventStore);
        actualGrainState.State.Constraints = (await eventStore.Constraints.GetDefinitions()).ToList();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ConstraintsState>)!;
        var key = (ConstraintsKey)grainId.Key.ToString()!;
        var eventStore = storage.GetEventStore(key.EventStore);

        foreach (var constraint in actualGrainState.State.Constraints)
        {
            await eventStore.Constraints.SaveDefinition(constraint);
        }
    }
}
