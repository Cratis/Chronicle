// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry;

[Collection(MongoDBCollection.Name)]
public class when_retrying_after_head_reuse(MongoDBFixture fixture) : ArchiveRetryConformance
{
    readonly string _name = $"registry_retry_{Guid.NewGuid():N}";
    IMongoClient _client;
    IEventStoreNamespaceDatabase _database;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_name);
        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads)
            .Returns(database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads));
        _database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory)
            .Returns(database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory));
    }

    protected override IEventSequenceMutationRegistry RecreateRegistry() => new EventSequenceMutationRegistry("store", "namespace", _database);

    Task Destroy() => _client.DropDatabaseAsync(_name);
}
