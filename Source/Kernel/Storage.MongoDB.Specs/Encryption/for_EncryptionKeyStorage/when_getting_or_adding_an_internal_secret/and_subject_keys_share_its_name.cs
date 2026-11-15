// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_subject_keys_share_its_name(ReplicaSetMongoDBFixture fixture) : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EncryptionKeyIdentifier _alias = InternalSecretPurpose.MutationReceiptDigest.Value;
    static readonly EncryptionKey _secret = new([1], [2]);

    IEncryptionKeyStorage _store;
    IEncryptionKeyStorage _racingStore;
    EncryptionKey _afterSubjectLifecycle;
    EncryptionKey _racingResult;
    bool _subjectKeyExists;

    async Task Establish()
    {
        // The event store database and the namespace database are one physical database, so the spec shows the
        // separation comes from the collections rather than from the databases being different. Key fences read with
        // linearizable read concern, which only a replica set serves (Chronicle requires one), hence the replica set fixture.
        var mongo = new MongoClient(fixture.ConnectionString).GetDatabase($"internal-secrets-{Guid.NewGuid():N}");
        _store = StoreOver(mongo);
        _racingStore = StoreOver(mongo);
        await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, _secret);
    }

    async Task Because()
    {
        await _store.GetOrAddFor(_eventStore, EventStoreNamespaceName.Default, _alias, new EncryptionKey([7], [7]));
        await _store.RecordErasureFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.DeleteFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        await _store.AllowNewKeyFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _subjectKeyExists = await _store.HasFor(_eventStore, EventStoreNamespaceName.Default, _alias);
        _afterSubjectLifecycle = await _store.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, new EncryptionKey([3], [4]));
        _racingResult = await _racingStore.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, new EncryptionKey([5], [6]));
    }

    [Fact] void should_have_erased_the_subject_key() => _subjectKeyExists.ShouldBeFalse();
    [Fact] void should_keep_the_internal_secret() => _afterSubjectLifecycle.Private.ShouldEqual(_secret.Private);
    [Fact] void should_give_another_silo_the_stored_secret() => _racingResult.Private.ShouldEqual(_secret.Private);

    static Cratis.Compliance.MongoDB.EncryptionKeyStorage StoreOver(IMongoDatabase mongo)
    {
        var database = Substitute.For<IDatabase>();
        var eventStore = Substitute.For<IEventStoreDatabase>();
        database.GetEventStoreDatabase(_eventStore).Returns(eventStore);
        eventStore.GetCollection<InternalSecretForPurpose>("internal-secrets").Returns(mongo.GetCollection<InternalSecretForPurpose>("internal-secrets"));
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<EncryptionKeyForIdentifier>("encryption-keys").Returns(mongo.GetCollection<EncryptionKeyForIdentifier>("encryption-keys"));
        namespaceDatabase.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures").Returns(mongo.GetCollection<EncryptionKeyErasureForIdentifier>("encryption-key-erasures"));
        eventStore.GetNamespaceDatabase(EventStoreNamespaceName.Default).Returns(namespaceDatabase);
        return new(database);
    }
}
