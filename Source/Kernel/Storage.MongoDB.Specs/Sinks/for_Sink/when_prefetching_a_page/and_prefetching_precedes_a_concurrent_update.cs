// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_prefetching_precedes_a_concurrent_update(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    BsonDocument _stored;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 0 }, { "__lastHandledEventSequenceNumber", -1L } });

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key]);
        await _collection.UpdateOneAsync(new BsonDocument("_id", "k"), Builders<BsonDocument>.Update.Set("count", 40).Set("concurrentField", "retained"));
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        await _sink.EndBulk();
        _stored = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }

    [Fact] void should_keep_the_concurrent_field() => _stored["concurrentField"].AsString.ShouldEqual("retained");
    [Fact] void should_apply_the_existing_property_update() => _stored["count"].AsInt32.ShouldEqual(1);
    [Fact] void should_not_change_watermark_planning() => _stored["__lastHandledEventSequenceNumber"].ToInt64().ShouldEqual(0L);
}
