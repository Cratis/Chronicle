// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_the_miss_is_cleared(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _beforeClear;
    ExpandoObject? _afterClear;

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key]);
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 } });
        _beforeClear = await _sink.FindOrDefault(_key);
        PrefetchSink.ClearPrefetchPage();
        _afterClear = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
    }

    [Fact] void should_use_the_page_miss_before_clearing() => _beforeClear.ShouldBeNull();
    [Fact] void should_read_again_after_clearing() => _afterClear.ShouldNotBeNull();
    [Fact] void should_perform_one_prefetch_and_one_fallback_read() => _reads.ShouldEqual(2);
}
