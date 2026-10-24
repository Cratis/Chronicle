// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_written_key_is_evicted_after_an_immediate_removal(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;

    async Task Because()
    {
        await PrefetchSink.PrefetchPage([_key]);
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        var removal = Changes(0);
        removal.Remove();
        await _sink.ApplyChangesImmediately(_key, removal, 1UL, SinkWriteMode.Always);
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 9 } });
        _state = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
    }

    [Fact] void should_discard_the_original_page_miss_on_eviction() => _state.ShouldNotBeNull();
    [Fact] void should_read_again_after_the_key_is_evicted() => _reads.ShouldEqual(2);
    [Fact] void should_observe_the_concurrent_insert_after_removal() => ((IDictionary<string, object>)_state!)["count"].ShouldEqual(9);
}
