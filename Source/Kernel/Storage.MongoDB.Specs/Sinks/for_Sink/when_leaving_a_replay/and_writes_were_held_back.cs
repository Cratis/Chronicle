// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_leaving_a_replay;

/// <summary>
/// A silo leaves a replay when another silo has already ended it and promoted the read model. Writes this silo still
/// holds back would land in a replay collection nothing reads any more, so they go to the read model instead.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class and_writes_were_held_back(MongoDBFixture fixture) : Contract.given.an_accumulating_read_model<MongoSinkHarness>
{
    const string ReplayContainerName = $"replay-{ContainerName}";

    readonly Key _heldBack = new("held-back", ArrayIndexers.NoIndexers);

    MongoSinkHarness _harness;
    bool _inReadModel;
    bool _inReplay;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { Fixture = fixture };

    async Task Establish()
    {
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_heldBack, ChangesetSettingCountTo(1), 1UL);
    }

    async Task Because()
    {
        await _sink.LeaveReplay();
        _inReadModel = await Contains(ContainerName);
        _inReplay = await Contains(ReplayContainerName);
    }

    [Fact] void should_write_what_was_held_back_to_the_read_model() => _inReadModel.ShouldBeTrue();
    [Fact] void should_not_write_it_to_the_replay_collection() => _inReplay.ShouldBeFalse();

    Task<bool> Contains(string collection) =>
        _harness.Database.GetCollection<BsonDocument>(collection).Find(Builders<BsonDocument>.Filter.Eq("_id", "held-back")).AnyAsync();
}
