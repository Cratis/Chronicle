// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_copying_collection_valued_child_keys : given.a_sink_with_gated_bulk_writes
{
    readonly int[] _identity = [42];
    readonly List<ExpandoObject> _list = [];
    bool _originalMatched;
    bool _indexedMatched;
    Type _childrenType;
    Type _identityType;
    Type _listType;
    int _cachedIdentity;
    int _cachedListCount;

    async Task Establish()
    {
        _holdFirstFlush = false;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
        await _sink.BeginBulk();
        var changeset = Changes(1);
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = _identity;
        _list.Add(child);
        ((IDictionary<string, object?>)changeset.CurrentState)["children"] = new[] { child };
        ((IDictionary<string, object?>)changeset.CurrentState)["list"] = _list;
        PropertyPath path = "[children].id";
        _originalMatched = Sink.TryFindValueInDocument(changeset.CurrentState, path.Segments.ToArray(), 0, "System.Int32[]");
        await _sink.ApplyChanges(_firstKey, changeset, 1);
    }

    async Task Because()
    {
        var result = await _sink.TryFindRootKeyByChildValue("[children].id", "System.Int32[]");
        _indexedMatched = result.TryGetValue(out var key) && key == _firstKey;
        _identity[0] = 99;
        _list.Clear();
        var returned = (IDictionary<string, object?>)(await _sink.FindOrDefault(_firstKey))!;
        _childrenType = returned["children"]!.GetType();
        _listType = returned["list"]!.GetType();
        var child = ((IEnumerable<ExpandoObject>)returned["children"]!).Single();
        var identity = ((IDictionary<string, object?>)child)["id"]!;
        _identityType = identity.GetType();
        _cachedListCount = ((List<ExpandoObject>)returned["list"]!).Count;
        ((int[])identity)[0] = 100;
        var secondRead = (IDictionary<string, object?>)(await _sink.FindOrDefault(_firstKey))!;
        var secondChild = ((IEnumerable<ExpandoObject>)secondRead["children"]!).Single();
        _cachedIdentity = ((int[])((IDictionary<string, object?>)secondChild)["id"]!)[0];
    }

    [Fact] void should_have_a_nonempty_match_in_the_original_scan() => _originalMatched.ShouldBeTrue();
    [Fact] void should_match_the_pre_copy_scan() => _indexedMatched.ShouldEqual(_originalMatched);
    [Fact] void should_preserve_the_child_array_type() => _childrenType.ShouldEqual(typeof(ExpandoObject[]));
    [Fact] void should_preserve_the_identity_array_type() => _identityType.ShouldEqual(typeof(int[]));
    [Fact] void should_preserve_the_list_type() => _listType.ShouldEqual(typeof(List<ExpandoObject>));
    [Fact] void should_copy_nested_array_values_on_write_and_read() => _cachedIdentity.ShouldEqual(42);
    [Fact] void should_copy_nested_list_values() => _cachedListCount.ShouldEqual(1);
}
