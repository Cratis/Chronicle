// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_the_bulk_session_ends(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key]);
        await _sink.EndBulk();
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 } });
        await _sink.BeginBulk();
        _state = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
    }

    [Fact] void should_discard_the_old_page_miss() => _state.ShouldNotBeNull();
    [Fact] void should_read_again_in_the_new_session() => _reads.ShouldEqual(2);
}
