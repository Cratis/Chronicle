// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_Storage;

public sealed class ControllableDatabase(IMongoCollection<EventStore> collection) : IDatabase, IDisposable
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    readonly StubEventStoreDatabase _eventStoreDatabase = new();
    readonly ManualResetEventSlim _firstCallerArrived = new();
    readonly ManualResetEventSlim _callersReleased = new();
    int _arrivals;
    int _eventStoreDatabaseCalls;

    public bool RendezvousOnGetEventStoreDatabase { get; set; }

    public bool CallersWereReleased => _callersReleased.IsSet;

    public int EventStoreDatabaseCalls => _eventStoreDatabaseCalls;

    public bool WaitForFirstCaller() => _firstCallerArrived.Wait(_timeout);

    public IMongoCollection<T> GetCollection<T>(string? collectionName = null) => (IMongoCollection<T>)(object)collection;

    public IEventStoreDatabase GetEventStoreDatabase(EventStoreName eventStore)
    {
        Interlocked.Increment(ref _eventStoreDatabaseCalls);
        if (RendezvousOnGetEventStoreDatabase)
        {
            Rendezvous();
        }

        return _eventStoreDatabase;
    }

    public IMongoDatabase GetReadModelDatabase(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        throw new NotSupportedException();

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

    sealed class StubEventStoreDatabase : IEventStoreDatabase
    {
        readonly Lock _collectionsLock = new();
        readonly Dictionary<Type, object> _collections = [];

        public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
        {
            lock (_collectionsLock)
            {
                if (!_collections.TryGetValue(typeof(T), out var collection))
                {
                    collection = Substitute.For<IMongoCollection<T>>();
                    _collections[typeof(T)] = collection;
                }

                return (IMongoCollection<T>)collection;
            }
        }

        public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace) => throw new NotSupportedException();
    }
}
