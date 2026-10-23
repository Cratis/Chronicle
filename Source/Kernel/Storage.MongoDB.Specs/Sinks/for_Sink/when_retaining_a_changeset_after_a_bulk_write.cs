// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_retaining_a_changeset_after_a_bulk_write : given.a_sink_with_gated_bulk_writes
{
    readonly List<(int Count, int ChildCount, bool OriginalFound, bool MutatedFound, bool AddedFound)> _results = [];

    void Establish()
    {
        _holdFirstFlush = false;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
    }

    async Task Because()
    {
        foreach (var immediate in new[] { false, true })
        {
            await _sink.BeginBulk();
            var changeset = Changes(42);
            var child = new ExpandoObject();
            ((IDictionary<string, object?>)child)["id"] = 42;
            var children = new List<ExpandoObject> { child };
            ((IDictionary<string, object?>)changeset.CurrentState)["children"] = children;
            await Write(changeset, immediate);
            await _sink.TryFindRootKeyByChildValue("[children].id", 42);
            ((IDictionary<string, object?>)changeset.CurrentState)["count"] = 99;
            ((IDictionary<string, object?>)child)["id"] = 99;
            var added = new ExpandoObject();
            ((IDictionary<string, object?>)added)["id"] = 100;
            children.Add(added);
            var cached = (IDictionary<string, object?>)(await _sink.FindOrDefault(_firstKey))!;
            var original = await _sink.TryFindRootKeyByChildValue("[children].id", 42);
            var mutated = await _sink.TryFindRootKeyByChildValue("[children].id", 99);
            var addition = await _sink.TryFindRootKeyByChildValue("[children].id", 100);
            _results.Add(((int)cached["count"]!, ((IEnumerable<object>)cached["children"]!).Count(), original.TryGetValue(out var key) && key == _firstKey, mutated.TryGetValue(out _), addition.TryGetValue(out _)));
        }
    }

    [Fact] void should_cover_buffered_and_immediate_writes() => _results.Count.ShouldEqual(2);
    [Fact] void should_keep_the_written_scalar() => _results.ShouldEachConformTo(result => result.Count == 42);
    [Fact] void should_keep_the_written_collection() => _results.ShouldEachConformTo(result => result.ChildCount == 1);
    [Fact] void should_find_the_written_child() => _results.ShouldEachConformTo(result => result.OriginalFound);
    [Fact] void should_not_find_a_retained_child_mutation() => _results.ShouldEachConformTo(result => !result.MutatedFound);
    [Fact] void should_not_find_a_retained_collection_addition() => _results.ShouldEachConformTo(result => !result.AddedFound);

    Task<IEnumerable<FailedPartition>> Write(Changeset<AppendedEvent, ExpandoObject> changeset, bool immediate) =>
        immediate ? _sink.ApplyChangesImmediately(_firstKey, changeset, 1, SinkWriteMode.Always) : _sink.ApplyChanges(_firstKey, changeset, 1);
}
