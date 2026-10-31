// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_or_adding_with_revision;

[Collection(MongoDBCollection.Name)]
public class and_the_key_exists(MongoDBFixture fixture) : Specification
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    EncryptionKey _expected = new([1], [2]);
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        var mongo = new MongoClient(fixture.ConnectionString).GetDatabase($"key-pairs-{Guid.NewGuid():N}");
        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        database.GetEventStoreDatabase(EventStoreName.NotSet).Returns(eventStore);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys").Returns(mongo.GetCollection<EncryptionKeyForIdentifier>("encryption-keys"));
        namespaceDatabase.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures").Returns(mongo.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures"));
        eventStore.GetNamespaceDatabase(EventStoreNamespaceName.NotSet).Returns(namespaceDatabase);
        _store = new Cratis.Compliance.MongoDB.EncryptionKeyStorage(database);
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _expected, new EncryptionKeyRevision(7u));
    }

    async Task Because() => _result = await _store.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKey([3], [4]));

    [Fact] void should_report_the_existing_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] void should_return_the_existing_key() => _result!.Key.Public.ShouldEqual(_expected.Public);
    [Fact] async Task should_not_mint_another_revision() => (await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKeyRevision(8u))).ShouldBeFalse();
}
