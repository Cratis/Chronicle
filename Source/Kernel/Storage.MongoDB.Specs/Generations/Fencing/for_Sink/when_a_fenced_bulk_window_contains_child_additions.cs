// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_fenced_bulk_window_contains_child_additions(ReplicaSetMongoDBFixture fixture) : given.a_capped_fenced_sink(fixture)
{
    readonly List<FailedPartition> _failures = [];
    long _count;
    long _receiptsBeforeEnd;
    BsonDocument[] _documents;

    async Task Establish() => await CreateSink("""
        {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"children":{"type":"array","items":{"type":"object","properties":{"childId":{"type":"string"}}}}}}
        """);

    async Task Because()
    {
        await _sink.BeginBulk();
        for (var index = 1; index <= 5; index++)
        {
            var key = new Key($"parent-{index}", ArrayIndexers.NoIndexers);
            _failures.AddRange(await _sink.ApplyChanges(key, SettingCount(index), (ulong)(index * 2)));
            var child = new ExpandoObject();
            ((IDictionary<string, object?>)child)["childId"] = "child";
            var indexers = new ArrayIndexers([new ArrayIndexer("children", "childId", "child")]);
            _failures.AddRange(await _sink.ApplyChanges(key, Changes(new ChildAdded(child, "children", "childId", "child", indexers)), (ulong)((index * 2) + 1)));
        }
        _receiptsBeforeEnd = await Receipts();
        _failures.AddRange(await _sink.EndBulk());
        _count = await _models.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _documents = (await _models.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToArray();
    }

    [Fact] void should_flush_automatically_at_the_document_cap() => (_receiptsBeforeEnd > 0).ShouldBeTrue();
    [Fact] void should_not_fail_non_idempotent_writes_in_ordinary_bulk_flushes() => _failures.ShouldBeEmpty();
    [Fact] void should_write_more_distinct_documents_than_fit_one_transaction() => _count.ShouldEqual(5L);
    [Fact] void should_push_each_child_exactly_once() => _documents.All(document => document["children"].AsBsonArray.Count == 1).ShouldBeTrue();
}
