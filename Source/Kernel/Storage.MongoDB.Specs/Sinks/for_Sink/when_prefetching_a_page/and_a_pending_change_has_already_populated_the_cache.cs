// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_pending_change_has_already_populated_the_cache(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;
    BsonDocument _stored;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 0 }, { "__lastHandledEventSequenceNumber", -1L } });

    async Task Because()
    {
        await _sink.BeginBulk();
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        await PrefetchSink.PrefetchPage([_key]);
        _state = await _sink.FindOrDefault(_key);
        await _collection.UpdateOneAsync(new BsonDocument("_id", "k"), Builders<BsonDocument>.Update.Set("concurrentField", "retained"));
        await _sink.EndBulk();
        _stored = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }

    [Fact] void should_keep_the_pending_state_instead_of_the_prefetched_state() => ((IDictionary<string, object>)_state!)["count"].ShouldEqual(1);
    [Fact] void should_keep_modifier_writes_instead_of_replacing_a_stale_document() => _stored["concurrentField"].AsString.ShouldEqual("retained");
    [Fact] void should_persist_the_property_update_once() => _stored["count"].AsInt32.ShouldEqual(1);
    [Fact] void should_persist_the_existing_watermark_plan() => _stored["__lastHandledEventSequenceNumber"].ToInt64().ShouldEqual(0L);
}
