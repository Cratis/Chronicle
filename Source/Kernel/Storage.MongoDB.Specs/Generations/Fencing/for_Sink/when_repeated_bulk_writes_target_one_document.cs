// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_repeated_bulk_writes_target_one_document(ReplicaSetMongoDBFixture fixture) : given.a_capped_fenced_sink(fixture)
{
    long _beforeEnd;
    long _afterEnd;

    async Task Establish() => await CreateSink("""
        {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"}}}
        """);

    async Task Because()
    {
        await _sink.BeginBulk();
        for (var index = 1; index <= 5; index++)
        {
            await _sink.ApplyChanges(new Key("one", ArrayIndexers.NoIndexers), SettingCount(index), (ulong)index);
        }
        _beforeEnd = await Receipts();
        await _sink.EndBulk();
        _afterEnd = await Receipts();
    }

    [Fact] void should_not_count_operations_as_distinct_documents() => _beforeEnd.ShouldEqual(0L);
    [Fact] void should_commit_one_batch() => _afterEnd.ShouldEqual(1L);
}
