// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintEncodingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_an_absent_key(ReplicaSetMongoDBFixture fixture) : given.a_key_storage(fixture)
{
    UniqueConstraintEncodingKey? _result;
    long _documents;

    async Task Because()
    {
        _result = await _storage.GetKey();
        _documents = await _database.GetCollection<BsonDocument>(CollectionName).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_return_null() => _result.ShouldBeNull();
    [Fact] void should_not_create_a_document() => _documents.ShouldEqual(0L);
}
