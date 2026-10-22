// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_removing;

[Collection(MongoDBCollection.Name)]
public class and_it_is_the_replay_shadow(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    Exception? _error;
    string? _replayed;

    [Fact] public void should_refuse() => _error.ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] public void should_keep_the_replay() => _replayed.ShouldEqual("replayed");

    protected override async Task Establish()
    {
        await FirstSilo.BeginReplay(ContextRevertingTo("things-revert"));
        await Write(FirstSilo, "replayed");
        _error = await Catch.Exception(() => FirstSilo.Remove(ReplayName));
        _replayed = await VersionIn(ReplayName);
    }
}
