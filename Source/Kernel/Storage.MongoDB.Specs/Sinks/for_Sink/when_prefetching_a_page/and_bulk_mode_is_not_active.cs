// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_bulk_mode_is_not_active(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    async Task Because()
    {
        await _sink.EndBulk();
        await PrefetchSink.PrefetchPage([_key]);
    }

    [Fact] void should_not_read_any_documents() => _reads.ShouldEqual(0);
}
