// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_mutating_a_bulk_answer : given.a_sink_with_gated_bulk_writes
{
    bool _originalValueFound;
    bool _mutatedValueFound;
    bool _savedValueFound;
    bool _independentCopies;
    int _cachedCount;
    int _cachedChildId;
    int _cachedChildCount;
    byte _cachedByte;

    async Task Establish()
    {
        _holdFirstFlush = false;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
        await _sink.BeginBulk();
        var changeset = Changes(42);
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = 42;
        ((IDictionary<string, object?>)changeset.CurrentState)["children"] = new List<ExpandoObject> { child };
        ((IDictionary<string, object?>)changeset.CurrentState)["bytes"] = "*"u8.ToArray();
        await _sink.ApplyChanges(_firstKey, changeset, 1);
        await _sink.TryFindRootKeyByChildValue("[children].id", 42);
    }

    async Task Because()
    {
        var workingState = (await _sink.FindOrDefault(_firstKey))!;
        var state = (IDictionary<string, object?>)workingState;
        state["count"] = 99;
        var children = (List<ExpandoObject>)state["children"]!;
        ((IDictionary<string, object?>)children[0])["id"] = 99;
        children.Add(new ExpandoObject());
        ((IList<byte>)state["bytes"]!)[0] = 99;
        var cachedCopy = (await _sink.FindOrDefault(_firstKey))!;
        var cached = (IDictionary<string, object?>)cachedCopy;
        _independentCopies = !ReferenceEquals(workingState, cachedCopy);
        _cachedCount = (int)cached["count"]!;
        var cachedChildren = (List<ExpandoObject>)cached["children"]!;
        _cachedChildCount = cachedChildren.Count;
        _cachedChildId = (int)((IDictionary<string, object?>)cachedChildren[0])["id"]!;
        _cachedByte = ((IList<byte>)cached["bytes"]!)[0];
        var originalValue = await _sink.TryFindRootKeyByChildValue("[children].id", 42);
        _originalValueFound = originalValue.TryGetValue(out var key) && key == _firstKey;
        var mutatedValue = await _sink.TryFindRootKeyByChildValue("[children].id", 99);
        _mutatedValueFound = mutatedValue.TryGetValue(out _);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, cachedCopy);
        changeset.ReplaceState(workingState, [new PropertyDifference("count", 42, 99)]);
        await _sink.ApplyChanges(_firstKey, changeset, 2);
        var savedValue = await _sink.TryFindRootKeyByChildValue("count", 99);
        _savedValueFound = savedValue.TryGetValue(out key) && key == _firstKey;
    }

    [Fact] void should_return_independent_working_copies() => _independentCopies.ShouldBeTrue();
    [Fact] void should_keep_the_cached_scalar() => _cachedCount.ShouldEqual(42);
    [Fact] void should_keep_the_cached_child_id() => _cachedChildId.ShouldEqual(42);
    [Fact] void should_keep_the_cached_child_collection() => _cachedChildCount.ShouldEqual(1);
    [Fact] void should_keep_the_cached_binary_value() => _cachedByte.ShouldEqual((byte)42);
    [Fact] void should_find_the_cache_owned_value() => _originalValueFound.ShouldBeTrue();
    [Fact] void should_not_find_unsaved_working_changes() => _mutatedValueFound.ShouldBeFalse();
    [Fact] void should_accept_the_working_state_through_an_explicit_write() => _savedValueFound.ShouldBeTrue();
}
