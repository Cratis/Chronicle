// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_within_bulk_root_state : given.a_bulk_child_removal_from_all
{
    object _matched;
    bool _missing;

    async Task Establish()
    {
        await Cache(Root("root-a"), new { children = new[] { new { childId = "shared" }, new { childId = "only-first" } } }.AsExpandoObject());
        await Cache(Root("root-b"), new { children = new[] { new { childId = "shared" } } }.AsExpandoObject());
        _collection.ClearReceivedCalls();
    }

    async Task Because()
    {
        var result = await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "shared", Root("root-b"));
        result.TryGetValue(out var key).ShouldBeTrue();
        _matched = key.Value;
        _missing = (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "only-first", Root("root-b"))).HasValue;
    }

    static Key Root(string id) => new(id, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_only_the_requested_cached_root() => _matched.ShouldEqual("root-b");
    [Fact] void should_not_select_a_child_from_another_cached_root() => _missing.ShouldBeFalse();
    [Fact]
    void should_not_query_stale_persisted_state() => _collection.DidNotReceive().FindAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
