// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkProvenanceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_after_restricted_inventory_is_lost(ReplicaSetMongoDBFixture fixture) : given.a_provenance_database(fixture)
{
    SinkProvenanceState _state;

    async Task Establish()
    {
        await _storage.CompleteInventory(true);
        await _metadata.WithWriteConcern(WriteConcern.WMajority).DeleteOneAsync(new BsonDocument("_id", "inventory"));
    }

    async Task Because() => _state = await new SinkProvenanceStorage(_database).ReadState();

    [Fact] void should_not_recover_completion() => _state.IsComplete.ShouldBeFalse();
    [Fact] void should_recover_restriction() => _state.Restricted.ShouldBeTrue();
}
