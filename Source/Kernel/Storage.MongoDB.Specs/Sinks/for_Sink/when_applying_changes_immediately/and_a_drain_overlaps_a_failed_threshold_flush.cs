// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_a_drain_overlaps_a_failed_threshold_flush(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    FailedPartition[] _reported;

    async Task Establish()
    {
        _holdFirstBatch = true;
        await RejectDuplicateCounts();
        await QueueBelowThreshold();
    }

    async Task Because()
    {
        var threshold = ReachThreshold();
        await _batchStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var immediate = _sink.ApplyChangesImmediately(_futureKey, Changes(1001), 1001UL, SinkWriteMode.Always);
        _releaseBatch.SetResult();
        var ownerFailures = await threshold;
        var waiterFailures = await immediate;
        var finalFailures = await _sink.EndBulk();
        _reported = ownerFailures.Concat(waiterFailures).Concat(finalFailures).ToArray();
    }

    [Fact] void should_hand_out_the_failed_partition_exactly_once() => _reported.Select(failure => failure.EventSourceId).ShouldContainOnly(_queuedKey);
    [Fact] async Task should_commit_the_immediate_write() => (await Document(_futureKey))!["count"].AsInt32.ShouldEqual(1001);
}
