// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

public sealed class MongoMaterializationBindingStorageHarness : IMaterializationBindingStorageHarness
{
    readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace), IMongoDatabase> _databases = [];
    MongoClient _client;
    public string ConnectionString { get; init; }
    public IGenerationGateWrites GateWrites { get; init; } = new MongoDBGenerationGateWrites();
    public IMaterializationBindingStorage Create(TimeProvider? timeProvider = null)
    {
        _client = new(ConnectionString);
        var database = Substitute.For<IDatabase>();
        database.GetReadModelDatabase(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call => Database(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1)));
        return new MongoDBMaterializationBindingStorage(database, GateWrites, timeProvider);
    }
    public IMongoDatabase Database(EventStoreName store, EventStoreNamespaceName ns)
    {
        if (!_databases.TryGetValue((store, ns), out var db))
        {
            db = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
            _databases.Add((store, ns), db);
        }
        return db;
    }
    public void Dispose()
    {
        foreach (var db in _databases.Values) _client.DropDatabase(db.DatabaseNamespace.DatabaseName);
        _client.Dispose();
    }
}
