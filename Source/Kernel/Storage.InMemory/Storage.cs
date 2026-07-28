// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IStorage"/>.
/// </summary>
/// <param name="eventStoreStorages">The shared <see cref="EventStoreStorages"/> registry.</param>
/// <param name="system">The <see cref="ISystemStorage"/> for system-level storage.</param>
public sealed class Storage(EventStoreStorages eventStoreStorages, ISystemStorage system) : IStorage
{
    /// <inheritdoc/>
    public ISystemStorage System { get; } = system;

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreName>> GetEventStores() => Task.FromResult(eventStoreStorages.Names);

    /// <inheritdoc/>
    public Task<bool> HasEventStore(EventStoreName eventStore) => Task.FromResult(eventStoreStorages.Has(eventStore));

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => eventStoreStorages.Observe;

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        ThrowIfEventStoreNameIsInvalid(eventStore);
        return eventStoreStorages.GetOrCreate(eventStore);
    }

    /// <inheritdoc/>
    public void Clear() => eventStoreStorages.Clear();

    static void ThrowIfEventStoreNameIsInvalid(EventStoreName eventStore)
    {
        if (eventStore is null || string.IsNullOrWhiteSpace(eventStore.Value))
        {
            throw new InvalidEventStoreName(eventStore!, "EventStoreName cannot be null, empty or whitespace.");
        }

        if (eventStore == EventStoreName.NotSet)
        {
            throw new InvalidEventStoreName(eventStore, "EventStoreName cannot be '[NotSet]'. It must be properly configured.");
        }
    }
}
