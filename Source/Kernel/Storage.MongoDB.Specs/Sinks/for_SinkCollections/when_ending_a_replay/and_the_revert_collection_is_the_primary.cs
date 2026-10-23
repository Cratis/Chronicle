// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_the_revert_collection_is_the_primary(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    Exception? _error;
    IReadOnlyList<string> _collections = [];
    string? _readModel;

    [Fact] public void should_refuse() => _error.ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] public void should_keep_the_primary_as_it_was() => _readModel.ShouldEqual("before");
    [Fact] public void should_keep_the_replay_to_promote_later() => _collections.ShouldContain(ReplayName);
    [Fact] public void should_not_claim_the_replay() => _collections.ShouldNotContain(PromotingName);

    protected override async Task Establish()
    {
        await FirstSilo.BeginReplay(ContextRevertingTo(ContainerName));
        await Write(FirstSilo, "replayed");
        _error = await Catch.Exception(() => FirstSilo.EndReplay(ContextRevertingTo(ContainerName)));
        _collections = await CollectionNames();
        _readModel = await VersionIn(ContainerName);
    }
}
