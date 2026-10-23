// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_composite_key_is_written_and_deleted_atomically(ReplicaSetMongoDBFixture fixture) : given.a_capped_fenced_sink(fixture)
{
    long _written;
    long _remaining;
    long _receipts;

    async Task Establish() => await CreateSink(
        """
        {"type":"object","properties":{"id":{"type":"object","properties":{"a":{"type":"integer"},"b":{"type":"string"}}},"count":{"type":"integer"}}}
        """,
        1);

    async Task Because()
    {
        await _writes.InBatch(async () =>
        {
            await _sink.ApplyChanges(CompositeKey(1), SettingCount(1), 1UL);
            await _sink.ApplyChanges(CompositeKey(1), SettingCount(2), 2UL);
            return true;
        });
        _written = await _models.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await _sink.ApplyChanges(CompositeKey(1), Changes(new Removed(new ExpandoObject())), 3UL);
        _remaining = await _models.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _receipts = await Receipts();
    }

    [Fact] void should_count_repeated_composite_keys_as_one_destination() => _written.ShouldEqual(1L);
    [Fact] void should_delete_by_the_composite_key() => _remaining.ShouldEqual(0L);
    [Fact] void should_keep_the_upserts_in_one_transaction() => _receipts.ShouldEqual(2L);
}
