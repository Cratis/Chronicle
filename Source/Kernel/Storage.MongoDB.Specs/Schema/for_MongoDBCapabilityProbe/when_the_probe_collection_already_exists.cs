// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_the_probe_collection_already_exists : given.a_driver
{
    void Establish()
    {
        _collections.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collections.Current.Returns([MongoDBCapabilityProbe.ProbeCollectionName]);
        _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(given.command_errors.Create(2, "createIndex.collation is not implemented yet")));
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_still_detect_unsupported_collation() => _report.Collation.State.ShouldEqual(MongoDBCapabilityState.Unsupported);
    [Fact] void should_create_an_independent_collection() => _createdCollections.Single().ShouldNotEqual(MongoDBCapabilityProbe.ProbeCollectionName);
    [Fact] void should_clean_up_only_its_own_collection() => _droppedCollections.Single().ShouldEqual(_createdCollections.Single());
}
