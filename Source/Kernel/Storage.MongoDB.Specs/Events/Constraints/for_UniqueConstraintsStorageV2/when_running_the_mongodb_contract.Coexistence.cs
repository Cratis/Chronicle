// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Fact]
    public async Task should_leave_every_v1_document_and_index_untouched()
    {
        const string Legacy = "sequence+constraint+constraint";
        await _database.CreateCollectionAsync(Legacy);
        var legacy = _database.GetCollection<BsonDocument>(Legacy);
        await legacy.InsertOneAsync(new BsonDocument { { "_id", "legacy-owner" }, { "Value", "legacy-value" }, { "Subject", "legacy-subject" } });
        await legacy.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("Value", 1), new CreateIndexOptions { Name = "value", Unique = true }));
        var documents = await Documents(Legacy);
        using var before = await legacy.Indexes.ListAsync();
        var indexes = (await before.ToListAsync()).Select(index => index.ToJson()).ToArray();
        await Seed();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        for (var operation = 1; operation < 6; operation++) (await Mutation(operation, _authority)).IsSuccess.ShouldBeTrue();
        (await Documents(Legacy)).SequenceEqual(documents).ShouldBeTrue();
        using var after = await legacy.Indexes.ListAsync();
        (await after.ToListAsync()).Select(index => index.ToJson()).SequenceEqual(indexes).ShouldBeTrue();
    }
}
