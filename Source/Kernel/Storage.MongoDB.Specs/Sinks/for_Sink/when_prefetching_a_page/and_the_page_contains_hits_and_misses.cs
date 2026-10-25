// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_the_page_contains_hits_and_misses(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _hit;
    ExpandoObject? _miss;
    BsonDocument _stored;
    readonly Key _missing = new("missing", ArrayIndexers.NoIndexers);

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 }, { "__lastHandledEventSequenceNumber", 15L } });

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key, _missing, _key]);
        _hit = await _sink.FindOrDefault(_key);
        _miss = await _sink.FindOrDefault(_missing);
        await _sink.EndBulk();
        _stored = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }

    [Fact] void should_seed_the_hit() => ((IDictionary<string, object>)_hit!)["count"].ShouldEqual(7);
    [Fact] void should_remember_the_miss() => _miss.ShouldBeNull();
    [Fact] void should_read_the_page_only_once() => _reads.ShouldEqual(1);
    [Fact] void should_not_issue_any_writes() => _writes.ShouldEqual(0);
    [Fact] void should_not_modify_the_document() => _stored["count"].AsInt32.ShouldEqual(7);
    [Fact] void should_not_advance_the_server_watermark() => _stored["__lastHandledEventSequenceNumber"].AsInt64.ShouldEqual(15L);
}
