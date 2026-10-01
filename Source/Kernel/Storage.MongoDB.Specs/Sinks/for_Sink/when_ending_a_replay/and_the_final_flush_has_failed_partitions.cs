// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// A partition the server rejects in the final flush of a replay is reported as failed, but it never keeps the sink in
/// replay mode. The sink is cached for the silo, so a sink left replaying sent every later live change to the replay
/// collection - which nothing reads - until the process restarted.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_final_flush_has_failed_partitions(MongoDBFixture fixture) : Contract.given.an_accumulating_read_model<MongoSinkHarness>
{
    const string ReplayContainerName = $"replay-{ContainerName}";

    readonly Key _accepted = new("accepted", ArrayIndexers.NoIndexers);
    readonly Key _rejected = new("rejected", ArrayIndexers.NoIndexers);
    readonly Key _live = new("live", ArrayIndexers.NoIndexers);

    MongoSinkHarness _harness;
    FailedPartition[] _failedPartitions;
    bool _liveChangeInReadModel;
    bool _liveChangeInReplay;
    bool _replayedChangePromoted;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { Fixture = fixture };

    async Task Establish()
    {
        await _sink.BeginReplay(ReplayContext());

        // Two partitions carrying the same value under a unique index: the server accepts the first and rejects the
        // second with a duplicate key error when the final flush writes them.
        await _harness.Database.GetCollection<BsonDocument>(ReplayContainerName).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("count"), new CreateIndexOptions { Unique = true }));
        await _sink.ApplyChanges(_accepted, ChangesetSettingCountTo(1), 1UL);
        await _sink.ApplyChanges(_rejected, ChangesetSettingCountTo(1), 2UL);
    }

    async Task Because()
    {
        _failedPartitions = (await _sink.EndReplay(ReplayContext())).ToArray();
        await _sink.ApplyChanges(_live, ChangesetSettingCountTo(2), 3UL);

        _liveChangeInReadModel = await Contains(ContainerName, "live");
        _liveChangeInReplay = await Contains(ReplayContainerName, "live");
        _replayedChangePromoted = await Contains(ContainerName, "accepted");
    }

    [Fact] void should_report_the_rejected_partition() => _failedPartitions.Single().EventSourceId.ShouldEqual(_rejected);
    [Fact] void should_report_the_event_that_failed() => _failedPartitions.Single().EventSequenceNumber.Value.ShouldEqual(2UL);
    [Fact] void should_report_why_the_server_rejected_it() => _failedPartitions.Single().Reason.ShouldContain("11000");
    [Fact] void should_promote_what_the_replay_wrote() => _replayedChangePromoted.ShouldBeTrue();
    [Fact] void should_write_later_changes_to_the_read_model() => _liveChangeInReadModel.ShouldBeTrue();
    [Fact] void should_not_write_later_changes_to_the_replay_collection() => _liveChangeInReplay.ShouldBeFalse();

    async Task<bool> Contains(string collection, string id) =>
        await _harness.Database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", id)) > 0;
}
