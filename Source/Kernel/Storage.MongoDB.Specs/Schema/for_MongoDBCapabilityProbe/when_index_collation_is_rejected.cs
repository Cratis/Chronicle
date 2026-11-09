// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_index_collation_is_rejected : given.a_driver
{
    void Establish() => _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(given.command_errors.Create(2, "createIndex.collation is not implemented yet")));

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_report_collation_as_unsupported() => _report.Collation.State.ShouldEqual(MongoDBCapabilityState.Unsupported);
    [Fact] void should_still_clean_up_the_collection() => _ = _mongo.Received(1).DropCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CancellationToken>());
}
