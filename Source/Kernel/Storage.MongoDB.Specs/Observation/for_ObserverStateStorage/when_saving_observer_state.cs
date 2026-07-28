// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage;

/// <summary>
/// Proves that saving observer state issues a targeted <c>$set</c> update rather than replacing the whole
/// document: the changed scalar is applied, while a field the observer state does not own — here the legacy
/// per-partition counts still awaiting migration — is left intact instead of being wiped by a full replace.
/// Runs against a real MongoDB.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_saving_observer_state(MongoDBFixture fixture) : Specification
{
    static readonly ObserverId _observerId = "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    const string PartitionOne = "partition-one";
    const string TypeA = "a1111111-1111-1111-1111-111111111111";

    static when_saving_observer_state() => BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());

    IMongoClient _client = default!;
    string _databaseName = default!;
    IMongoDatabase _database = default!;
    IEventStoreNamespaceDatabase _namespaceDatabase = default!;
    ObserverStateStorage _storage = default!;
    ObserverState _documentAfterSave = default!;

    async Task Establish()
    {
        _databaseName = $"chronicle_observer_save_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(_databaseName);

        _namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        _namespaceDatabase.GetObserverStateCollection()
            .Returns(_database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers));
        _namespaceDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts)
            .Returns(_database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts));

        _storage = new ObserverStateStorage(_namespaceDatabase);

        // Seed a document with a field the targeted update does not touch. A full replace would wipe it.
        var existing = new ObserverState
        {
            Id = _observerId,
            NextEventSequenceNumber = 5,
            RunningState = ObserverRunningState.Disconnected,
            HandledEventCountPerPartition = new Dictionary<string, IDictionary<string, ulong>>
            {
                { PartitionOne, new Dictionary<string, ulong> { { TypeA, 7 } } }
            }
        };
        await _database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers).InsertOneAsync(existing);
    }

    async Task Because()
    {
        var state = new KernelObserverState(
            _observerId,
            EventSequenceNumber.Unavailable,
            ObserverRunningState.Active,
            new HashSet<Key>(),
            new HashSet<Key>(),
            [],
            FailedPartitionCount.Zero,
            false,
            false)
        {
            NextEventSequenceNumber = 99
        };

        await _storage.Save(state);

        _documentAfterSave = await _database
            .GetCollection<ObserverState>(WellKnownCollectionNames.Observers)
            .Find(Builders<ObserverState>.Filter.Eq(_ => _.Id, _observerId))
            .FirstAsync();
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    [Fact] void should_apply_the_updated_next_event_sequence_number() => _documentAfterSave.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)99UL);

    [Fact] void should_apply_the_updated_running_state() => _documentAfterSave.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_not_replace_the_document_wiping_untouched_fields() => _documentAfterSave.HandledEventCountPerPartition[PartitionOne][TypeA].ShouldEqual(7UL);
}
