// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage;

public class when_the_external_authority_is_standalone : Specification
{
    IMongoCollection<BsonDocument> _collection;
    EncryptionKeyStorage _storage;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(_collection);
        _collection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(_collection);
        _collection.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(_collection);
        var state = EncryptionKeyFenceState.Empty.Serialize();
        var document = new BsonDocument { { "version", new Decimal128(0m) }, { "state", state }, { "seal", EncryptionKeyFenceState.Seal(state) } };
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(document)));
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(1, 1, null));
        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventStoreDatabase(EventStoreName.NotSet).Returns(eventStore);
        eventStore.GetNamespaceDatabase(EventStoreNamespaceName.NotSet).Returns(namespaceDatabase);
        namespaceDatabase.GetCollection<BsonDocument>("EncryptionKeyExternalFences").Returns(_collection);
        var topology = Substitute.For<IMongoDBTopology>();
        topology.IsStandalone.Returns(true);
        _storage = new(database, topology);
    }

    async Task Because()
    {
        var state = await _storage.ReadFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");
        var next = state with { Erasure = state.Erasure with { Version = state.Erasure.Version + 1 } };
        await _storage.CompareExchangeFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", state, next);
    }

    [Fact] void should_not_advertise_durability() => _storage.IsDurable.ShouldBeFalse();
    [Fact] void should_read_linearly() => _collection.Received().WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_read_from_the_primary() => _collection.Received().WithReadPreference(ReadPreference.Primary);
    [Fact] void should_never_downgrade_reads() => _collection.DidNotReceive().WithReadConcern(ReadConcern.Default);
    [Fact] void should_write_with_majority_journal_and_timeout() => _collection.Received().WithWriteConcern(Arg.Is<WriteConcern>(_ => _.W.Equals(WriteConcern.WMajority.W) && _.Journal == true && _.WTimeout != null));
    [Fact] void should_never_downgrade_writes() => _collection.DidNotReceive().WithWriteConcern(WriteConcern.Acknowledged);
}
