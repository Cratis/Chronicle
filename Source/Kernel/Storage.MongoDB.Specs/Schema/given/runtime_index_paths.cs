// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.MongoDB.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB.Alerts;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.MongoDB.Occurrences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.given;

/// <summary>
/// Exercises every runtime ensure path against already provisioned collections.
/// </summary>
internal static class runtime_index_paths
{
    /// <summary>
    /// Exercises the runtime stores without direct index DDL.
    /// </summary>
    /// <param name="database">The routed Chronicle database.</param>
    /// <param name="jobs">The real jobs database.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">A runtime jobs storage operation failed.</exception>
    internal static async Task Exercise(IDatabase database, IMongoDatabase jobs)
    {
        var store = database.GetEventStoreDatabase("orders");
        var ns = store.GetNamespaceDatabase("Default");
        await ns.EnsureIndexesForEventSequence(EventSequenceId.Log);
        await new EventSequenceStorage(
            "orders",
            "Default",
            EventSequenceId.Log,
            ns,
            Substitute.For<IEventConverter>(),
            Substitute.For<IEventTypesStorage>(),
            Substitute.For<IIdentityStorage>(),
            Substitute.For<Json.IExpandoObjectConverter>(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            NullLogger<EventSequenceStorage>.Instance).EnsureIndexes();
        var constraints = new UniqueConstraintsStorage(ns, EventSequenceId.Log, NullLogger<UniqueConstraintsStorage>.Instance);
        await constraints.Save(EventSourceId.New(), new("email", []), 0, "one");
        await constraints.Save(EventSourceId.New(), new("retained-email", []) { Mode = UniqueConstraintMode.PerValue }, 1, "two");
        await new ClosedStreamsConstraintStorage(ns, EventSequenceId.Log).GetDimensionsInUse();
        await new FailedPartitionStorage(ns).GetFor((ObserverId?)null);
        await new InFlightEventsStorage(ns).GetFor("observer");
        await new AlertIncidentsStorage(ns).GetOpenCountsByObserver();
        await KeysWithFence(database, "orders", "Default").HasFor("orders", "Default", "absent");
        await KeysWithFence(database, EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace)
            .HasFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, "absent");
        await new OccurrencesStorage(ns.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database).Record(Occurrence());
        await new OccurrencesStorage(store.GetCollection<BsonDocument>(WellKnownCollectionNames.Namespaces).Database).Record(Occurrence());
        await ExerciseRows(ns);
        var jobsResult = await new JobStorage(jobs, Substitute.For<IJobTypes>()).GetJobs();
        if (jobsResult.TryGetException(out var jobsError)) throw StorageFailure("GetJobs", jobsError);
        var stepsResult = await new JobStepStorage(jobs).GetForJob(JobId.New());
        if (stepsResult.TryGetException(out var stepsError)) throw StorageFailure("GetForJob", stepsError);
    }

    static EncryptionKeyStorage KeysWithFence(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName ns)
    {
        // These schema fixtures use standalone MongoDB, which cannot serve linearizable fence reads.
        // Keep the actual key collection and its runtime index checks; fake only the fence authority.
        var realNamespace = database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(ns);
        var keys = realNamespace.GetCollection<EncryptionKeyForIdentifier>(WellKnownCollectionNames.EncryptionKeys);
        var fences = Substitute.For<IMongoCollection<EncryptionKeyErasureForIdentifier>>();
        fences.WithReadConcern(Arg.Any<ReadConcern>()).Returns(fences);
        fences.WithReadPreference(Arg.Any<ReadPreference>()).Returns(fences);
        fences.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(new EncryptionKeyErasureForIdentifier("absent", 0u, [], true))));
        var routedNamespace = Substitute.For<IEventStoreNamespaceDatabase>();
        routedNamespace.GetCollection<EncryptionKeyForIdentifier>(WellKnownCollectionNames.EncryptionKeys).Returns(keys);
        routedNamespace.GetCollection<EncryptionKeyErasureForIdentifier>(WellKnownCollectionNames.EncryptionKeyErasures).Returns(fences);
        var routedStore = Substitute.For<IEventStoreDatabase>();
        routedStore.GetNamespaceDatabase(ns).Returns(routedNamespace);
        var routedDatabase = Substitute.For<IDatabase>();
        routedDatabase.GetEventStoreDatabase(eventStore).Returns(routedStore);
        return new(routedDatabase);
    }

    static InvalidOperationException StorageFailure(string operation, Exception error) => new(
        $"{operation} failed: {error.GetType().Name}, MongoDB code {(error is MongoCommandException command ? command.Code : 0)}");

    static Occurrence Occurrence() => new(
        new(Guid.NewGuid().ToString()),
        "trigger",
        OccurrenceKind.Application,
        "fingerprint",
        "{}",
        1,
        DateTimeOffset.UtcNow,
        null,
        null,
        OccurrenceState.Pending,
        0,
        0,
        null,
        null,
        null,
        null);

    static async Task ExerciseRows(IEventStoreNamespaceDatabase database)
    {
        var observer = new ObserverKey("observer", "orders", "Default", EventSequenceId.Log);
        var key = new ObserverDefinitionChangeRowKey(observer, new(1), "row");
        var job = JobId.New();
        (await new ObserverDefinitionChangeRows<ObserverProgressRecord>(database).TryInsert(
            new(key, 0, ObserverDefinitionChangeRowState.Started, new(job, "partition", ObserverWorkOutcome.Checkpoint, DateTimeOffset.UtcNow), new(10)))).ShouldBeTrue();
        (await new ObserverDefinitionChangeRows<ObserverContinuation>(database).TryInsert(
            new(key, 0, ObserverDefinitionChangeRowState.Intended, new("partition", job), new(10)))).ShouldBeTrue();
        (await new ObserverDefinitionChangeRows<ObserverRestartPlan>(database).TryInsert(
            new(key, 0, ObserverDefinitionChangeRowState.Intended, new(job, "partition", new(1), new(100), ObserverReaderScope.Explicit([new("event-a", new(2))])), new(10)))).ShouldBeTrue();
        (await new ObserverDefinitionChangeRows<ObserverJobInventory>(database).TryInsert(
            new(key, 0, ObserverDefinitionChangeRowState.Intended, new(job, "CatchUpObserver", ObserverDefinitionChangeJobKind.CatchUp, new(false, "continuation")), new(10)))).ShouldBeTrue();
    }
}
