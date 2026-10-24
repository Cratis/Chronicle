// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_resuming_a_bulk_window;

public class and_a_replay_window_is_open : given.a_sink_with_gated_bulk_writes
{
    BulkFlushResult _opened;
    BulkFlushResult _afterResuming;
    ExpandoObject _cachedState;

    async Task Establish()
    {
        _holdFirstFlush = false;
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(cursor);
        await _sink.ResumeReplay(new ReplayContext(new ReadModelType("bulk-model", ReadModelGeneration.First), "bulk", "revert-bulk", DateTimeOffset.UtcNow));
        _opened = await _sink.FlushBulk();
        await Apply(_firstKey, 1, 1UL);
    }

    async Task Because()
    {
        await _sink.ResumeBulk();
        _cachedState = await _sink.FindOrDefault(_firstKey);
        await Apply(_secondKey, 2, 2UL);
        _afterResuming = await _sink.FlushBulk();
    }

    [Fact] void should_replace_the_replay_window() => _afterResuming.WindowId.ShouldNotEqual(_opened.WindowId);
    [Fact] void should_not_carry_replay_writes_into_catchup() => WrittenValuesFor(_firstKey).ShouldBeEmpty();
    [Fact] void should_clear_the_replay_bulk_state() => _cachedState.ShouldBeNull();
    [Fact] void should_write_the_catchup_changes() => WrittenValuesFor(_secondKey).ShouldContainOnly(2);
    [Fact] void should_target_the_primary_collection() => _collections.GetCollection().CollectionNamespace.CollectionName.ShouldEqual("BulkModel");
}
