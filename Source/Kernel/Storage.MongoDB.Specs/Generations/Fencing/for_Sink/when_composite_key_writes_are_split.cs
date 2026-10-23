// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_composite_key_writes_are_split(ReplicaSetMongoDBFixture fixture) : given.a_capped_fenced_sink(fixture)
{
    Exception? _error;
    long _remaining;
    long _receipts;

    async Task Establish() => await CreateSink(
        """
        {"type":"object","properties":{"id":{"type":"object","properties":{"a":{"type":"integer"},"b":{"type":"string"}}},"count":{"type":"integer"}}}
        """,
        1);

    async Task Because()
    {
        _error = await Catch.Exception(() => _writes.InBatch(async () =>
        {
            for (var index = 1; index <= 3; index++)
            {
                await _sink.ApplyChanges(CompositeKey(index), SettingCount(index), (ulong)index);
                await _sink.ApplyChanges(CompositeKey(index), Changes(new Removed(new ExpandoObject())), (ulong)index);
            }
            return true;
        }));
        _remaining = await _models.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _receipts = await Receipts();
    }

    [Fact] void should_accept_composite_key_equality_as_split_safe() => _error.ShouldBeNull();
    [Fact] void should_delete_every_upserted_document() => _remaining.ShouldEqual(0L);
    [Fact] void should_commit_the_split_writes() => _receipts.ShouldEqual(6L);
}
