// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_watching_a_container;

/// <summary>
/// A replay promotion first renames the container aside and then renames the rebuilt collection into its place.
/// Only the second rename may be reported: answering the first would read the container while it does not exist
/// and hand every observer an empty page between the old state and the replayed one.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> supplying the replica set the change stream needs.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_replay_is_promoted_into_it(ReplicaSetMongoDBFixture fixture) : given.a_watched_container(fixture.ConnectionString)
{
    const string PromotingName = $"replay-{ContainerName}-promoting";
    const string RevertName = $"{ContainerName}-revert";

    string[] _operations;

    async Task Establish() =>
        await _database.GetCollection<BsonDocument>(PromotingName).InsertOneAsync(new BsonDocument("_id", "replayed"));

    async Task Because()
    {
        await _database.RenameCollectionAsync(ContainerName, RevertName);
        await _database.RenameCollectionAsync(PromotingName, ContainerName);
        await InsertMarker();
        _operations = await OperationsUpToTheMarker();
    }

    [Fact] void should_report_only_the_rename_into_the_container() => _operations.ShouldEqual(["rename", "insert"]);
}
