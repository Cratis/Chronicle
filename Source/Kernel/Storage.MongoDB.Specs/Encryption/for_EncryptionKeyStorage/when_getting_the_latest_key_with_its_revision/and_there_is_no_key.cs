// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

[Collection(MongoDBCollection.Name)]
public class and_there_is_no_key(MongoDBFixture fixture) : Specification
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    EncryptionKeyAtRevision? _result;

    void Establish()
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
    }

    async Task Because() => _result = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    [Fact] void should_report_absence() => _result.ShouldBeNull();
    [Fact] async Task should_not_provision_a_key() => (await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)).ShouldBeFalse();
}
