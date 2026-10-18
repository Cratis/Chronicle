// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_non_identity_unique_index_is_violated(ReplicaSetMongoDBFixture fixture) : given.a_keyed_batch_storage(fixture)
{
    Exception _error;

    async Task Establish()
    {
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Event>(Builders<Event>.IndexKeys.Ascending(item => item.EventSourceId), new CreateIndexOptions { Unique = true, Name = "unrelated_unique" }));
        (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _error = await Catch.Exception(() => _storage.AppendManyWithIdentities([_event with { SequenceNumber = 1, Identity = new EventPublication("new", "intent") }]));

    [Fact] void should_surface_the_unrelated_unique_violation() => _error.ShouldBeOfExactType<MongoBulkWriteException<Event>>();
    [Fact] async Task should_not_record_the_attempted_identity() => (await _storage.TryGetPublications(["new"])).Count.ShouldEqual(0);
}
