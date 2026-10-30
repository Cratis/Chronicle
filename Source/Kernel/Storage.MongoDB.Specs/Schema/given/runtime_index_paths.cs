// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.MongoDB.Occurrences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Compliance.MongoDB;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB.Jobs;
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
        await new EncryptionKeyStorage(database).HasFor("orders", "Default", "absent");
        await new EncryptionKeyStorage(database).HasFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, "absent");
        await new OccurrencesStorage(ns.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database).Record(Occurrence());
        await new OccurrencesStorage(store.GetCollection<BsonDocument>(WellKnownCollectionNames.Namespaces).Database).Record(Occurrence());
        await ExerciseRows(ns);
        (await new JobStorage(jobs, Substitute.For<IJobTypes>()).GetJobs()).IsSuccess.ShouldBeTrue();
        (await new JobStepStorage(jobs).GetForJob(JobId.New())).IsSuccess.ShouldBeTrue();
    }

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
