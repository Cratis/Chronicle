// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling event store subscriptions state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class EventStoreSubscriptionsManagerStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventStoreSubscriptionsState>)!;
        var eventStore = storage.GetEventStore(grainId.Key.ToString()!);
        actualGrainState.State.Subscriptions = await eventStore.EventStoreSubscriptions.GetAll();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<EventStoreSubscriptionsState>)!;
        var eventStoreSubscriptions = storage.GetEventStore(grainId.Key.ToString()!).EventStoreSubscriptions;

        var existing = await eventStoreSubscriptions.GetAll();
        var existingIds = existing.Select(s => s.Identifier).ToHashSet();
        var updatedIds = actualGrainState.State.Subscriptions.Select(s => s.Identifier).ToHashSet();

        foreach (var removedId in existingIds.Where(id => !updatedIds.Contains(id)))
        {
            await eventStoreSubscriptions.Delete(removedId);
        }

        foreach (var subscription in actualGrainState.State.Subscriptions)
        {
            await eventStoreSubscriptions.Save(subscription);
        }
    }
}
