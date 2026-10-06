// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_a_removal_arrives_during_the_final_flush : given.a_sink_with_gated_bulk_writes
{
    bool _completedDuringFlush;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        var removal = Changes(0);
        removal.Remove();
        var lateRemoval = _sink.ApplyChanges(_firstKey, removal, 2UL);
        _completedDuringFlush = lateRemoval.IsCompleted;
        _releaseFirstFlush.SetResult();
        await ending;
        await lateRemoval;
    }

    [Fact] void should_delete_directly_after_the_flush() => _collection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>());
    [Fact] void should_wait_for_closure_before_deleting() => _completedDuringFlush.ShouldBeFalse();
}
