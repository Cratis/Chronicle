// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_the_probe_collection_already_exists : given.a_driver
{
    void Establish()
    {
        _collections.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        _collections.Current.Returns([MongoDBCapabilityProbe.ProbeCollectionName]);
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_be_inconclusive() => _report.Collation.State.ShouldEqual(MongoDBCapabilityState.Inconclusive);
    [Fact] void should_not_create_the_collection_again() => _ = _mongo.DidNotReceiveWithAnyArgs().CreateCollectionAsync(default(string)!, default!, default);
    [Fact] void should_not_modify_indexes() => _ = _indexes.DidNotReceiveWithAnyArgs().CreateOneAsync(default(CreateIndexModel<BsonDocument>)!, default!, default);
    [Fact] void should_not_remove_a_pre_existing_collection() => _ = _mongo.DidNotReceiveWithAnyArgs().DropCollectionAsync(default(string)!, default(CancellationToken));
}
