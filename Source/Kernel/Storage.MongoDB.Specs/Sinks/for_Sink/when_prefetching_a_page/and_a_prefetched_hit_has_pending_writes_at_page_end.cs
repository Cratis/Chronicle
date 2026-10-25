// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_prefetched_hit_has_pending_writes_at_page_end(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;
    BsonDocument _stored;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 } });

    async Task Because()
    {
        await PrefetchSink.PrefetchPage([_key]);
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        PrefetchSink.ClearPrefetchPage();
        _state = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
        _stored = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }

    [Fact] void should_keep_the_pending_state_after_clearing_the_page() => ((IDictionary<string, object>)_state!)["count"].ShouldEqual(1);
    [Fact] void should_not_read_over_the_pending_state() => _reads.ShouldEqual(1);
    [Fact] void should_persist_the_pending_write() => _stored["count"].AsInt32.ShouldEqual(1);
}
