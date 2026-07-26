// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.for_Storage;

public sealed class ControllableClusterStorage : IClusterStorage, IDisposable
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    readonly ConcurrentDictionary<EventStoreName, bool> _saved = new();
    readonly ConcurrentQueue<IEventStoreStorage> _available = new();
    readonly ConcurrentQueue<IEventStoreStorage> _createdStorages = new();
    readonly ManualResetEventSlim _firstCallerArrived = new();
    readonly ManualResetEventSlim _callersReleased = new();
    int _arrivals;

    public bool RendezvousOnGetEventStores { get; set; }

    public bool CallersWereReleased => _callersReleased.IsSet;

    public IEnumerable<IEventStoreStorage> CreatedStorages => _createdStorages;

    public void Provide(IEventStoreStorage storage) => _available.Enqueue(storage);

    public bool WaitForFirstCaller() => _firstCallerArrived.Wait(_timeout);

    public Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        if (RendezvousOnGetEventStores)
        {
            Rendezvous();
        }

        return Task.FromResult<IEnumerable<EventStoreName>>([.. _saved.Keys]);
    }

    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => new Subject<IEnumerable<EventStoreName>>();

    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory)
    {
        if (!_available.TryDequeue(out var storage))
        {
            throw new InvalidOperationException("The spec did not provide enough event store storage instances.");
        }

        _createdStorages.Enqueue(storage);
        return storage;
    }

    public Task SaveEventStore(EventStoreName eventStore)
    {
        _saved.TryAdd(eventStore, true);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _firstCallerArrived.Dispose();
        _callersReleased.Dispose();
    }

    void Rendezvous()
    {
        if (Interlocked.Increment(ref _arrivals) == 1)
        {
            _firstCallerArrived.Set();
            _callersReleased.Wait(_timeout);
        }
        else
        {
            _callersReleased.Set();
        }
    }
}
