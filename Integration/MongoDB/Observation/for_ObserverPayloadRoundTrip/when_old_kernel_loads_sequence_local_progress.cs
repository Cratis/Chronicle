// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NSubstitute;
using CamelCaseElementNameConvention = Cratis.Chronicle.Storage.MongoDB.CamelCaseElementNameConvention;
using KernelCheckpoint = Cratis.Chronicle.Storage.Observation.ObserverSequenceCheckpoint;
using MongoCheckpoint = Cratis.Chronicle.Storage.MongoDB.Observation.ObserverSequenceCheckpoint;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Checks released-kernel readiness with tagged failures and a separate parked checkpoint present.
/// </summary>
public class when_old_kernel_loads_sequence_local_progress : Cratis.Specifications.Specification
{
    ReleasedKernelFixture _fixture;
    IMongoDatabase _database;
    BsonDocument _checkpointBefore;
    BsonDocument _checkpointAfter;
    string _identifier;
    string _parkedSequence;
    bool _ready;

    async Task Establish()
    {
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        ConventionRegistry.Register(
            "ReleasedObserverSequenceProgress",
            new ConventionPack { new CamelCaseElementNameConvention(_ => true) },
            type => type.Namespace?.StartsWith("Cratis.Chronicle.Storage", StringComparison.Ordinal) == true || type.Namespace?.StartsWith("Cratis.Chronicle.Concepts", StringComparison.Ordinal) == true);
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }
        if (!BsonClassMap.IsClassMapRegistered(typeof(MongoCheckpoint)))
        {
            BsonClassMap.RegisterClassMap<MongoCheckpoint>(new ObserverSequenceCheckpointClassMap().Configure);
        }

        _fixture = new();
        await _fixture.Start();
        var selected = _fixture.Observer;
        _database = _fixture.Client.GetDatabase(selected.Database);
        _identifier = selected.Identifier;
        _parkedSequence = $"{selected.Owner.Value}-parked";
        await _fixture.StopKernel();
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions)
            .Returns(_database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions));
        database.GetCollection<MongoCheckpoint>(WellKnownCollectionNames.ObserverSequenceCheckpoints)
            .Returns(_database.GetCollection<MongoCheckpoint>(WellKnownCollectionNames.ObserverSequenceCheckpoints));
        var failures = new FailedPartitionStorage(database);
        await failures.Save(_identifier, new FailedPartitions
        {
            Partitions =
            [
                new() { ObserverId = _identifier, Partition = "tagged-partition", EventSequenceId = selected.Owner, Attempts = [new() { SequenceNumber = new(1), Messages = ["seeded failure"] }] },
                new() { ObserverId = _identifier, Partition = "untagged-partition", Attempts = [new() { SequenceNumber = new(1), Messages = ["seeded failure"] }] }
            ]
        });
        var checkpoints = new ObserverSequenceCheckpointsStorage(database);
        await checkpoints.Save(KernelCheckpoint.Fresh(_identifier, _parkedSequence) with
        {
            NextEventSequenceNumber = new(43),
            LastHandledEventSequenceNumber = new(42),
            TailEventSequenceNumber = new(80),
            FailedPartitionCount = new(1),
            Parked = DateTimeOffset.UnixEpoch.AddMinutes(10)
        });
        _checkpointBefore = await CheckpointDocument();
    }

    async Task Because()
    {
        // The tag round trip awaits a released-kernel failing-partition seed hook in #5368.
        // Existing readiness checks persisted observer presence, not an old-writer update.
        await _fixture.RestartKernel();
        _ready = true;
        await _fixture.StopKernel();
        _checkpointAfter = await CheckpointDocument();
    }

    [Fact] void should_pass_the_released_observer_readiness_wait() => _ready.ShouldBeTrue();
    [Fact] void should_leave_the_checkpoint_document_unchanged() => _checkpointAfter.ShouldEqual(_checkpointBefore);

    async Task Cleanup()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
    }

    Task<BsonDocument> CheckpointDocument() =>
        _database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverSequenceCheckpoints)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", new ObserverSequenceCheckpointId(_identifier, _parkedSequence).ToBsonDocument()))
            .SingleAsync();
}
