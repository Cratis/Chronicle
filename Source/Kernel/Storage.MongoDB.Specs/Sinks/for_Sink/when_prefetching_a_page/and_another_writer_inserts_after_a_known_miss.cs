// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_another_writer_inserts_after_a_known_miss(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    BsonDocument _prefetched;
    BsonDocument _baseline;
    readonly Key _baselineKey = new("baseline", ArrayIndexers.NoIndexers);

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key]);
        (await _sink.FindOrDefault(_key)).ShouldBeNull();
        await _collection.InsertManyAsync(new[] { "k", "baseline" }.Select(id => new BsonDocument { { "_id", id }, { "count", 40 }, { "concurrentField", "retained" }, { "__lastHandledEventSequenceNumber", -1L } }));
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        await _sink.ApplyChanges(_baselineKey, Changes(1), 0UL);
        await _sink.EndBulk();
        _prefetched = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
        _baseline = await _collection.Find(new BsonDocument("_id", "baseline")).SingleAsync();
        _prefetched.Remove("_id");
        _baseline.Remove("_id");
    }

    [Fact] void should_produce_the_same_document_as_without_prefetch() => _prefetched.ShouldEqual(_baseline);
    [Fact] void should_keep_the_concurrent_field() => _prefetched["concurrentField"].AsString.ShouldEqual("retained");
    [Fact] void should_apply_the_existing_property_update() => _prefetched["count"].AsInt32.ShouldEqual(1);
    [Fact] void should_use_the_existing_watermark_plan() => _prefetched["__lastHandledEventSequenceNumber"].ToInt64().ShouldEqual(0L);
}
