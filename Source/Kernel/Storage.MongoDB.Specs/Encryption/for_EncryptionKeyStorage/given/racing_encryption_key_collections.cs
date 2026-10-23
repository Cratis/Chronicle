// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;

public class racing_encryption_key_collections : Specification
{
    protected IMongoCollection<EncryptionKeyForIdentifier> _keys;
    protected IMongoCollection<EncryptionKeyErasureForIdentifier> _erasures;
    protected EncryptionKeyStorage _storage;

    void Establish()
    {
        _keys = Substitute.For<IMongoCollection<EncryptionKeyForIdentifier>>();
        _erasures = Substitute.For<IMongoCollection<EncryptionKeyErasureForIdentifier>>();
        _keys.CollectionNamespace.Returns(new CollectionNamespace("specs", "encryption-keys"));
        _keys.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyForIdentifier, EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf<EncryptionKeyForIdentifier>()));
        _erasures.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf<EncryptionKeyErasureForIdentifier>()));

        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventStoreDatabase(EventStoreName.NotSet).Returns(eventStore);
        eventStore.GetNamespaceDatabase(EventStoreNamespaceName.NotSet).Returns(namespaceDatabase);
        namespaceDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys").Returns(_keys);
        namespaceDatabase.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures").Returns(_erasures);
        _storage = new(database);
    }
}
