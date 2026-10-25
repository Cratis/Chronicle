// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

public sealed class MongoMaterializationBindingStorageHarness : IMaterializationBindingStorageHarness
{
    readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace), IMongoDatabase> _databases = [];
    MongoClient _client;
    IDatabase _database;
    IObserverIdentityClaims _claims;
    public string ConnectionString { get; init; }
    public IGenerationGateWrites GateWrites { get; init; } = new MongoDBGenerationGateWrites();
    public IObserverIdentityClaims Claims => _claims ??= new MongoDBObserverIdentityClaims(StorageDatabase());
    public IMaterializationBindingStorage Create(TimeProvider? timeProvider = null) => new MongoDBMaterializationBindingStorage(StorageDatabase(), GateWrites, timeProvider, Claims);
    public Task SaveObserverDefinition(EventStoreName store, ObserverId observerId) =>
        Database(store, "definitions").GetCollection<BsonDocument>(WellKnownCollectionNames.Observers).InsertOneAsync(new BsonDocument("_id", observerId.Value));
    public IMongoDatabase Database(EventStoreName store, EventStoreNamespaceName ns)
    {
        lock (_databases)
        {
            if (!_databases.TryGetValue((store, ns), out var db))
            {
                db = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
                _databases.Add((store, ns), db);
            }
            return db;
        }
    }
    IDatabase StorageDatabase()
    {
        if (_database is not null) return _database;
        _client ??= new(ConnectionString);
        var database = Substitute.For<IDatabase>();
        database.GetReadModelDatabase(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call => Database(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1)));
        database.GetEventStoreDatabase(Arg.Any<EventStoreName>()).Returns(call =>
        {
            var store = call.Arg<EventStoreName>();
            var eventStore = Substitute.For<IEventStoreDatabase>();
            eventStore.GetCollection<Observation.ObserverDefinition>(WellKnownCollectionNames.Observers).Returns(Database(store, "definitions").GetCollection<Observation.ObserverDefinition>(WellKnownCollectionNames.Observers));
            eventStore.GetCollection<BsonDocument>(MongoDBObserverIdentityClaims.CollectionName).Returns(Database(store, "definitions").GetCollection<BsonDocument>(MongoDBObserverIdentityClaims.CollectionName));
            return eventStore;
        });
        return _database = database;
    }
    public void Dispose()
    {
        foreach (var db in _databases.Values) _client.DropDatabase(db.DatabaseNamespace.DatabaseName);
        _client.Dispose();
    }
}
