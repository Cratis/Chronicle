// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_beginning_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_the_replay_shadow_has_documents(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    string? _shadow;
    string? _readModel;

    [Fact] public void should_clear_the_replay_shadow() => _shadow.ShouldBeNull();
    [Fact] public void should_leave_the_primary_untouched() => _readModel.ShouldEqual("before");

    protected override async Task Establish()
    {
        await Database.GetCollection<BsonDocument>(ReplayName).InsertOneAsync(new BsonDocument { { "_id", "thing" }, { "version", "stale" } });
        await FirstSilo.BeginReplay(ContextRevertingTo("things-revert"));
        _shadow = await VersionIn(ReplayName);
        _readModel = await VersionIn(ContainerName);
    }
}
