// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk;

[Collection(MongoDBCollection.Name)]
public class and_bulk_mode_is_not_active(MongoDBFixture fixture) : when_prefetching_a_page.given.a_prefetch_sink(fixture)
{
    Option<Key> _result;

    async Task Because()
    {
        await _sink.EndBulk();
        _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("count"), 7);
    }

    [Fact] void should_ask_mongo() => _reads.ShouldEqual(1);
    [Fact] void should_not_find_anything() => _result.TryGetValue(out _).ShouldBeFalse();
}
