// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreInitialization"/>.
/// </summary>
/// <param name="storage">The storage for event stores.</param>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class EventStoreInitialization(IStorage storage, IGrainFactory grainFactory, ILogger<EventStoreInitialization> logger) : IEventStoreInitialization
{
    readonly ConditionalWeakTable<IEventStoreStorage, Lazy<Task>> _initializations = new();

    /// <inheritdoc/>
    public Task Ensure(EventStoreName eventStore)
    {
        if (CaseInsensitiveEventStoreNameComparer.Instance.Equals(eventStore, EventStoreName.System)) return Task.CompletedTask;
        var store = storage.GetEventStore(eventStore);
        return _initializations.GetValue(store, store => new Lazy<Task>(() => Initialize(eventStore, store))).Value;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> EnsureAll(IEnumerable<EventStoreName> eventStores)
    {
        var results = await Task.WhenAll(eventStores.Select(async eventStore => (EventStore: eventStore, Initialized: await TryEnsure(eventStore))));
        return results.Where(result => result.Initialized).Select(result => result.EventStore);
    }

    /// <inheritdoc/>
    public void Clear() => _initializations.Clear();

    async Task Initialize(EventStoreName eventStore, IEventStoreStorage store)
    {
        try
        {
            if (await store.IsInitialized()) return;
            await grainFactory.GetGrain<IEventStoreInitializer>(eventStore.Value).Ensure();
        }
        catch
        {
            _initializations.Remove(store);
            throw;
        }
    }

    async Task<bool> TryEnsure(EventStoreName eventStore)
    {
        try
        {
            await Ensure(eventStore);
            return true;
        }
        catch (Exception exception)
        {
            logger.EventStoreInitializationFailed(eventStore, exception);
            return false;
        }
    }
}
