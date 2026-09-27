// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_a_replay;

/// <summary>
/// A swap that cannot complete - here because the revert name is already taken - fails loudly, keeps the
/// replay so it can still be promoted, and never leaves the sink writing to the replay collection: that is
/// what froze read models in production without a single error to show for it (#4296).
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_swap_fails(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    Exception? _error;
    IReadOnlyList<string> _collections = [];
    string? _readModel;

    [Fact] public void should_fail() => _error.ShouldNotBeNull();
    [Fact] public void should_keep_the_replay_to_promote_later() => _collections.ShouldContain(ReplayName);
    [Fact] public void should_leave_no_claimed_collection_behind() => _collections.ShouldNotContain(PromotingName);
    [Fact] public void should_write_live_changes_to_the_read_model() => _readModel.ShouldEqual("live");

    protected override async Task Establish()
    {
        await Database.GetCollection<BsonDocument>("things-taken").InsertOneAsync(new BsonDocument("_id", "old"));
        await FirstSilo.BeginReplay(ContextRevertingTo("things-taken"));
        await Write(FirstSilo, "replayed");

        try
        {
            await FirstSilo.EndReplay(ContextRevertingTo("things-taken"));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _collections = await CollectionNames();

        await Write(FirstSilo, "live");
        _readModel = await VersionIn(ContainerName);
    }
}
