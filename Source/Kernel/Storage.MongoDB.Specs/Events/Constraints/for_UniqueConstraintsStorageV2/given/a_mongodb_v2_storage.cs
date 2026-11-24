// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public abstract class a_mongodb_v2_storage(ReplicaSetMongoDBFixture fixture) : UniqueConstraintsStorageV2Contract
{
    protected readonly EventSequenceId _sequence = "sequence";
    protected IMongoDatabase _database = null!;
    protected V2Interleaving _gates = null!;
    MongoClient _client = null!;

    protected override Task<IUniqueConstraintsStorageV2> CreateStorage()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
        _gates = new();
        return Task.FromResult(Handle());
    }

    protected IUniqueConstraintsStorageV2 Handle(EventSequenceId? sequence = null, IMongoDatabase? database = null) =>
        new UniqueConstraintsStorageV2(_gates.Forward(database ?? _database), sequence ?? _sequence);

    private protected V2Binding Binding(bool perValue = false) => new(_sequence.Value, _definition.Name.Value, perValue ? PerValue.Mode : _definition.Mode);

    protected override Task<IUniqueConstraintsStorageV2> ReopenStorage() => Task.FromResult(Handle());

    protected override async Task DestroyStorage()
    {
        _gates.Release();
        _gates.BeforeCall = null;
        _gates.AfterCall = null;
        await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
        _client.Dispose();
    }

    protected override Task<Result<bool, UniqueConstraintWriteFailure>> Interleave(Func<Task> transition, Func<Task<Result<bool, UniqueConstraintWriteFailure>>> mutation, bool transitionFirst) => _gates.Run(transition, mutation, transitionFirst);

    protected async Task<string[]> Catalog()
    {
        using var cursor = await _database.ListCollectionNamesAsync();
        return (await cursor.ToListAsync()).Order(StringComparer.Ordinal).ToArray();
    }

    protected async Task<string[]> Documents(string collection)
    {
        var rows = await _database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return rows.Select(row => row.ToJson()).Order(StringComparer.Ordinal).ToArray();
    }
}
