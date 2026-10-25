// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_prefetched_hit_is_cleared(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 } });

    async Task Because()
    {
        await PrefetchSink.PrefetchPage([_key]);
        await _collection.UpdateOneAsync(new BsonDocument("_id", "k"), Builders<BsonDocument>.Update.Set("count", 9));
        PrefetchSink.ClearPrefetchPage();
        _state = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
    }

    [Fact] void should_not_reuse_an_untouched_hit_from_the_old_page() => ((IDictionary<string, object>)_state!)["count"].ShouldEqual(9);
    [Fact] void should_read_the_document_again() => _reads.ShouldEqual(2);
}
