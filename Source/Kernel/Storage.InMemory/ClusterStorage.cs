// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IClusterStorage"/>.
/// </summary>
/// <param name="eventStoreStorages">The shared <see cref="EventStoreStorages"/> registry.</param>
public sealed class ClusterStorage(EventStoreStorages eventStoreStorages) : IClusterStorage
{
    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreName>> GetEventStores() => Task.FromResult(eventStoreStorages.Names);

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => eventStoreStorages.Observe;

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory) =>
        eventStoreStorages.GetOrCreate(eventStore, sinksFactory);

    /// <inheritdoc/>
    public Task SaveEventStore(EventStoreName eventStore)
    {
        eventStoreStorages.GetOrCreate(eventStore);
        return Task.CompletedTask;
    }
}
