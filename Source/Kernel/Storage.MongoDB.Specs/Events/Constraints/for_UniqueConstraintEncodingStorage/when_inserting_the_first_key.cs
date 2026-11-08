// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintEncodingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_inserting_the_first_key(ReplicaSetMongoDBFixture fixture) : given.a_key_storage(fixture)
{
    UniqueConstraintEncodingKey _candidate;
    UniqueConstraintEncodingKey _result;
    UniqueConstraintEncodingKey _stored;
    BsonDocument _raw;

    void Establish()
    {
        var material = new byte[32];
        material[0] = 0x00;
        material[1] = 0xFF;
        material[31] = 0xFF;
        _candidate = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("00ff10a5b6c7d8e9f0123456789abcde"), new UniqueConstraintStoreKey(material));
    }

    async Task Because()
    {
        _result = await _storage.CreateKeyIfAbsent(_candidate);
        _stored = (await _storage.GetKey())!;
        _raw = await _database.GetCollection<BsonDocument>(CollectionName).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
    }

    [Fact] void should_return_the_candidate() => _result.ShouldEqual(_candidate);
    [Fact] void should_read_the_same_key_id() => _stored.KeyId.ShouldEqual(_candidate.KeyId);
    [Fact] void should_read_identical_material() => Assert.Equal(_candidate.Material.CopyBytes(), _stored.Material.CopyBytes());
    [Fact] void should_store_the_key_id_as_sixteen_generic_binary_bytes() => (_raw["keyId"].AsBsonBinaryData.SubType, _raw["keyId"].AsBsonBinaryData.Bytes.Length).ShouldEqual((BsonBinarySubType.Binary, 16));
    [Fact] void should_store_the_material_as_thirty_two_generic_binary_bytes() => (_raw["material"].AsBsonBinaryData.SubType, _raw["material"].AsBsonBinaryData.Bytes.Length).ShouldEqual((BsonBinarySubType.Binary, 32));
}
