// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_a_replay;

/// <summary>
/// Ending a replay reaches every silo at once. Exactly one of them promotes the replay; none may move
/// away the collection another just promoted, and every one of them goes back to writing the read model
/// itself (#4296).
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/>.</param>
[Collection(MongoDBCollection.Name)]
public class and_every_silo_ends_it_at_once(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    IReadOnlyList<string> _collections = [];
    string? _promoted;
    string? _writtenAfterwards;

    [Fact] public void should_promote_the_replay() => _promoted.ShouldEqual("replayed");
    [Fact] public void should_leave_no_replay_collection_behind() => _collections.ShouldNotContain(ReplayName);
    [Fact] public void should_leave_no_claimed_collection_behind() => _collections.ShouldNotContain(PromotingName);
    [Fact] public void should_keep_exactly_one_revert_collection() => _collections.Count(name => name.StartsWith("things-", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] public void should_have_every_silo_write_to_the_read_model_again() => _writtenAfterwards.ShouldEqual("live");

    protected override async Task Establish()
    {
        await Task.WhenAll(FirstSilo.BeginReplay(ContextRevertingTo("things-first")), SecondSilo.BeginReplay(ContextRevertingTo("things-second")));
        await Write(FirstSilo, "replayed");

        await Task.WhenAll(FirstSilo.EndReplay(ContextRevertingTo("things-first")), SecondSilo.EndReplay(ContextRevertingTo("things-second")));
        _collections = await CollectionNames();
        _promoted = await VersionIn(ContainerName);

        await Write(SecondSilo, "live");
        _writtenAfterwards = await VersionIn(ContainerName);
    }
}
