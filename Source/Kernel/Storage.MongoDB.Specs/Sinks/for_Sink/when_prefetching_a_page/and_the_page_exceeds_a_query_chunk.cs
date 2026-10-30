// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_the_page_exceeds_a_query_chunk(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    Key[] _keys;
    int _hits;

    async Task Establish()
    {
        _keys = Enumerable.Range(0, 1001).Select(index => new Key($"key-{index}", ArrayIndexers.NoIndexers)).ToArray();
        await _collection.InsertManyAsync(Enumerable.Range(0, 1001).Select(index => new BsonDocument { { "_id", $"key-{index}" }, { "count", index } }));
    }

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage(_keys);
        foreach (var key in _keys)
        {
            if (await _sink.FindOrDefault(key) is not null) _hits++;
        }
        await _sink.EndBulk();
    }

    [Fact] void should_seed_all_chunks() => _hits.ShouldEqual(1001);
    [Fact] void should_issue_one_query_per_chunk_and_no_single_reads() => _reads.ShouldEqual(2);
}
