// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_indexes_are_ensured(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    BsonDocument? _index;

    async Task Because() => _index = (await (await _collection.Indexes.ListAsync()).ToListAsync()).Find(_ => _["name"] == "publication_identity");

    [Fact] void should_have_a_publication_identity_index_before_the_first_publication() => _index.ShouldNotBeNull();
    [Fact] void should_make_it_unique() => _index!["unique"].AsBoolean.ShouldBeTrue();
    [Fact] void should_make_it_sparse_so_ordinary_events_are_not_indexed() => _index!["sparse"].AsBoolean.ShouldBeTrue();
}
