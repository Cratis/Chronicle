// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_a_replay;

/// <summary>
/// A promotion cut short after claiming the replay collection leaves the claimed collection behind. The
/// next replay must still be able to promote rather than tripping over it.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_cut_short_promotion_was_left_behind(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    IReadOnlyList<string> _collections = [];
    string? _promoted;

    [Fact] public void should_promote_the_replay() => _promoted.ShouldEqual("replayed");
    [Fact] public void should_leave_no_claimed_collection_behind() => _collections.ShouldNotContain(PromotingName);

    protected override async Task Establish()
    {
        await FirstSilo.BeginReplay(ContextRevertingTo("things-revert"));
        await Write(FirstSilo, "replayed");
        await Database.GetCollection<BsonDocument>(PromotingName).InsertOneAsync(new BsonDocument { { "_id", "thing" }, { "version", "abandoned" } });

        await FirstSilo.EndReplay(ContextRevertingTo("things-revert"));
        _collections = await CollectionNames();
        _promoted = await VersionIn(ContainerName);
    }
}
