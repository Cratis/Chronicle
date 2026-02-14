// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Seeding;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="EventSeeds"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class EventSeedingGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        // Event seeding data is not typically cleared
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = EventSeedingKey.Parse(grainId.Key.ToString()!);
        var actualGrainState = (grainState as IGrainState<EventSeeds>)!;

        var eventSeedingStorage = GetEventSeedingStorage(key);
        actualGrainState.State = await eventSeedingStorage.Get();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = EventSeedingKey.Parse(grainId.Key.ToString()!);
        var actualGrainState = (grainState as IGrainState<EventSeeds>)!;

        var eventSeedingStorage = GetEventSeedingStorage(key);
        await eventSeedingStorage.Save(actualGrainState.State);
    }

    IEventSeedingStorage GetEventSeedingStorage(EventSeedingKey key)
    {
        if (key.IsGlobal)
        {
            // Global seeds are stored at event store level
            return storage.GetEventStore(key.EventStore).EventSeeding;
        }

        // Namespace-specific seeds are stored in the namespace database
        return storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).EventSeeding;
    }
}
