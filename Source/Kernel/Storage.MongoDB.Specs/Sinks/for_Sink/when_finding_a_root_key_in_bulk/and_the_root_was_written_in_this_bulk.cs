// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk;

[Collection(MongoDBCollection.Name)]
public class and_the_root_was_written_in_this_bulk(MongoDBFixture fixture) : when_prefetching_a_page.given.a_prefetch_sink(fixture)
{
    Option<Key> _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, Changes(7), 0UL);
        _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("count"), 7);
        await _sink.EndBulk();
    }

    [Fact] void should_resolve_the_written_root() => _result.TryGetValue(out _).ShouldBeTrue();
    [Fact] void should_not_read_from_mongo() => _reads.ShouldEqual(0);
}
