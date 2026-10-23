// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_created_instance_is_flushed(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    ExpandoObject? _state;

    async Task Because()
    {
        await _sink.BeginBulk();
        await PrefetchSink.PrefetchPage([_key]);
        await _sink.ApplyChanges(_key, Changes(1), 0UL);
        await _sink.FlushBulk();
        _state = await _sink.FindOrDefault(_key);
        await _sink.EndBulk();
    }

    [Fact] void should_discard_the_miss_when_the_write_is_flushed() => _state.ShouldNotBeNull();
    [Fact] void should_read_the_created_instance_after_the_bulk_cache_is_evicted() => _reads.ShouldEqual(2);
}
