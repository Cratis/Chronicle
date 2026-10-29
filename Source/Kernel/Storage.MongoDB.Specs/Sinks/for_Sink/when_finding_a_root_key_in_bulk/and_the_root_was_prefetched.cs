// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk;

[Collection(MongoDBCollection.Name)]
public class and_the_root_was_prefetched(MongoDBFixture fixture) : when_prefetching_a_page.given.a_prefetch_sink(fixture)
{
    Option<Key> _result;
    int _readsByTheLookup;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 9 } });

    async Task Because()
    {
        await PrefetchSink.PrefetchPage([_key]);
        var readsAfterPrefetch = _reads;
        _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("count"), 9);
        _readsByTheLookup = _reads - readsAfterPrefetch;
        await _sink.EndBulk();
    }

    [Fact] void should_resolve_the_prefetched_root() => _result.TryGetValue(out _).ShouldBeTrue();
    [Fact] void should_not_read_from_mongo_for_the_lookup() => _readsByTheLookup.ShouldEqual(0);
}
