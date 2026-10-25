// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NSubstitute;

using CamelCaseElementNameConvention = Cratis.Chronicle.Storage.MongoDB.CamelCaseElementNameConvention;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;
using MongoObserverState = Cratis.Chronicle.Storage.MongoDB.Observation.ObserverState;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Requires an observed old-writer update of an existing kernel-owned row, not merely a successful restart.
/// </summary>
public class when_old_kernel_reads_and_writes_new_claims : Cratis.Specifications.Specification
{
    ReleasedKernelFixture _fixture;
    IMongoDatabase _database;
    ObserverStateStorage _storage;
    KernelObserverState _before;
    KernelObserverState _after;
    BsonDocument _documentBefore;
    BsonDocument _documentAfter;
    string _identifier;
    bool _oldWriterUpdated;

    async Task Establish()
    {
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        ConventionRegistry.Register("ReleasedObserverClaims", new ConventionPack { new CamelCaseElementNameConvention(_ => true) },
            type => type.Namespace?.StartsWith("Cratis.Chronicle.Storage", StringComparison.Ordinal) == true || type.Namespace?.StartsWith("Cratis.Chronicle.Concepts", StringComparison.Ordinal) == true);
        _fixture = new();
        await _fixture.Start();
        var selected = _fixture.Observer;
        _database = _fixture.Client.GetDatabase(selected.Database);
        _identifier = selected.Identifier;
        await _fixture.StopKernel();
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetObserverStateCollection().Returns(_database.GetCollection<MongoObserverState>(WellKnownCollectionNames.Observers));
        _storage = new(database);
        _before = await _storage.Get(_identifier);
        await _storage.Save(_before with
        {
            PartitionClaims = [new("shadow-partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            OwnershipRevision = new(7)
        }, selected.Owner);
        _documentBefore = await Document();
        // Database profiling witnesses the old process's actual update of this exact row after restart.
        await _database.RunCommandAsync<BsonDocument>(new BsonDocument { { "profile", 2 }, { "slowms", 0 } });
    }

    async Task Because()
    {
        await _fixture.RestartKernel();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var filter = Builders<BsonDocument>.Filter.Eq("op", "update") &
            Builders<BsonDocument>.Filter.Eq("ns", $"{_database.DatabaseNamespace.DatabaseName}.{WellKnownCollectionNames.Observers}") &
            Builders<BsonDocument>.Filter.Eq("o2._id", _documentBefore["_id"]);
        try
        {
            using var updates = await _database.GetCollection<BsonDocument>("system.profile").FindAsync(filter,
                new FindOptions<BsonDocument> { CursorType = CursorType.TailableAwait, MaxAwaitTime = TimeSpan.FromSeconds(1) }, deadline.Token);
            while (await updates.MoveNextAsync(deadline.Token))
            {
                if (updates.Current.Any())
                {
                    _oldWriterUpdated = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            // The assertion below reports the missing old-writer update; timeout is never accepted as a pass.
        }
        await _fixture.StopKernel();
        _after = await _storage.Get(_identifier);
        _documentAfter = await Document();
    }

    [Fact] void should_require_an_actual_old_writer_update() => _oldWriterUpdated.ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_identity() => _after.Identifier.ShouldEqual(_before.Identifier);
    [Fact] void should_preserve_legacy_subscription_mode() => _after.SubscribesToAllEvents.ShouldEqual(_before.SubscribesToAllEvents);
    [Fact] void should_preserve_the_last_handled_event() => _after.LastHandledEventSequenceNumber.ShouldEqual(_before.LastHandledEventSequenceNumber);
    [Fact] void should_preserve_the_next_event() => _after.NextEventSequenceNumber.ShouldEqual(_before.NextEventSequenceNumber);
    [Fact] void should_preserve_handled_count() => _after.HandledEventCount.ShouldEqual(_before.HandledEventCount);
    [Fact] void should_preserve_legacy_replay_sets() => _after.ReplayingPartitions.ShouldContainOnly(_before.ReplayingPartitions);
    [Fact] void should_preserve_legacy_partition_sets() => _after.CatchingUpPartitions.ShouldContainOnly(_before.CatchingUpPartitions);
    [Fact] void should_preserve_partition_claims_byte_for_byte() => ShadowBytes(_documentAfter, nameof(MongoObserverState.PartitionClaims)).ShouldEqual(ShadowBytes(_documentBefore, nameof(MongoObserverState.PartitionClaims)));
    [Fact] void should_preserve_the_wide_claim_byte_for_byte() => ShadowBytes(_documentAfter, nameof(MongoObserverState.ObserverWideClaim)).ShouldEqual(ShadowBytes(_documentBefore, nameof(MongoObserverState.ObserverWideClaim)));
    [Fact] void should_preserve_the_revision() => _after.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_remain_readable_by_the_new_provider() => _after.PartitionClaims.Single().Epoch.ShouldEqual(new ClaimEpoch(3));

    async Task Cleanup()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
    }

    Task<BsonDocument> Document() => _database.GetCollection<BsonDocument>(WellKnownCollectionNames.Observers)
        .Find(Builders<BsonDocument>.Filter.Eq("_id", _identifier)).SingleAsync();

    static byte[] ShadowBytes(BsonDocument document, string property) => new BsonDocument("value", document[BsonClassMap.LookupClassMap(typeof(MongoObserverState)).GetMemberMap(property).ElementName]).ToBson();

}
