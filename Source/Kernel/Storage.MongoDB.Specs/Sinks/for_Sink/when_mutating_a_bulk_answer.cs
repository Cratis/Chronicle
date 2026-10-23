// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_mutating_a_bulk_answer : given.a_sink_with_gated_bulk_writes
{
    bool _newValueFound;
    bool _oldValueFound;
    bool _laterValueFound;
    bool _rewrittenValueFound;

    async Task Establish()
    {
        _holdFirstFlush = false;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
        await _sink.BeginBulk();
        await Apply(_firstKey, 42, 1);
        await _sink.TryFindRootKeyByChildValue("count", 42);
    }

    async Task Because()
    {
        var state = (IDictionary<string, object?>)(await _sink.FindOrDefault(_firstKey))!;
        state["count"] = 99;
        var oldValue = await _sink.TryFindRootKeyByChildValue("count", 42);
        _oldValueFound = oldValue.TryGetValue(out _);
        var newValue = await _sink.TryFindRootKeyByChildValue("count", 99);
        _newValueFound = newValue.TryGetValue(out var key) && key == _firstKey;
        state["count"] = 100;
        var laterValue = await _sink.TryFindRootKeyByChildValue("count", 100);
        _laterValueFound = laterValue.TryGetValue(out key) && key == _firstKey;
        await Apply(_firstKey, 101, 2);
        var rewrittenState = (IDictionary<string, object?>)(await _sink.FindOrDefault(_firstKey))!;
        rewrittenState["count"] = 102;
        var rewrittenValue = await _sink.TryFindRootKeyByChildValue("count", 102);
        _rewrittenValueFound = rewrittenValue.TryGetValue(out key) && key == _firstKey;
    }

    [Fact] void should_not_return_the_old_value() => _oldValueFound.ShouldBeFalse();
    [Fact] void should_find_the_mutated_value() => _newValueFound.ShouldBeTrue();
    [Fact] void should_observe_repeated_mutations_of_the_same_answer() => _laterValueFound.ShouldBeTrue();
    [Fact] void should_observe_mutation_after_rewriting_a_cached_document() => _rewrittenValueFound.ShouldBeTrue();
}
