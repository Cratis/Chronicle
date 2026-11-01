// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_prefetched_state_is_evicted : given.a_bulk_sink_with_cached_children
{
    Key? _before;
    Key? _after;

    async Task Establish()
    {
        _readDocuments = [new BsonDocument { { "_id", "first" }, { "children", new BsonArray { new BsonDocument("childId", "prefetched") } } }];
        await _sink.PrefetchPage([_first]);
        _before = await Find("prefetched");
        _readDocuments = [];
    }

    async Task Because()
    {
        _sink.ClearPrefetchPage();
        _after = await Find("prefetched");
    }

    [Fact] void should_find_the_prefetched_root() => _before.ShouldEqual(_first);
    [Fact] void should_not_resurrect_evicted_roots() => _after.ShouldBeNull();
}
