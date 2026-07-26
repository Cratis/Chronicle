// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

public sealed class ControllableEventStoreDatabase : IEventStoreDatabase, IDisposable
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    readonly StubNamespaceDatabase _namespaceDatabase = new();
    readonly Lock _collectionsLock = new();
    readonly Dictionary<Type, object> _collections = [];
    readonly ManualResetEventSlim _firstCallerArrived = new();
    readonly ManualResetEventSlim _callersReleased = new();
    int _arrivals;
    int _namespaceDatabaseCalls;

    public bool RendezvousOnGetNamespaceDatabase { get; set; }

    public bool CallersWereReleased => _callersReleased.IsSet;

    public int NamespaceDatabaseCalls => _namespaceDatabaseCalls;

    public bool WaitForFirstCaller() => _firstCallerArrived.Wait(_timeout);

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

    public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace)
    {
        Interlocked.Increment(ref _namespaceDatabaseCalls);
        if (RendezvousOnGetNamespaceDatabase)
        {
            Rendezvous();
        }

        return _namespaceDatabase;
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

    sealed class StubNamespaceDatabase : IEventStoreNamespaceDatabase
    {
        readonly Lock _collectionsLock = new();
        readonly Dictionary<Type, object> _collections = [];
        public IMongoClient Client { get; } = Substitute.For<IMongoClient>();

        public IMongoCollection<T> GetCollection<T>(string? name = default)
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

        public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId) => GetCollection<Event>();

        public IMongoCollection<BsonDocument> GetEventSequenceCollectionAsBsonFor(EventSequenceId eventSequenceId) => GetCollection<BsonDocument>();

        public IMongoCollection<ObserverState> GetObserverStateCollection() => GetCollection<ObserverState>();

        public Task EnsureIndexesForEventSequence(EventSequenceId eventSequenceId) => Task.CompletedTask;
    }
}
