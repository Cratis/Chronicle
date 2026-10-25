// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Alerts;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Observation;
using Cratis.Chronicle.Storage.MongoDB.Occurrences;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Enumerates kernel-owned indexes using the same typed models as the runtime.
/// </summary>
internal static class MongoDBIndexCatalogue
{
    /// <summary>
    /// Gets the indexes at event-store level.
    /// </summary>
    /// <param name="database">The event-store database.</param>
    /// <returns>The index definitions.</returns>
    internal static IReadOnlyList<MongoDBIndexDefinition> ForEventStore(IMongoDatabase database) =>
    [
        MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>("occurrences"), OccurrencesStorage.Indexes)
    ];

    /// <summary>
    /// Gets the indexes in the reserved global encryption scope.
    /// </summary>
    /// <param name="database">The database resolved through the runtime's fixed scope.</param>
    /// <returns>The index definitions.</returns>
    internal static IReadOnlyList<MongoDBIndexDefinition> ForEncryption(IMongoDatabase database) =>
    [
        MongoDBIndexDefinition.For(database.GetCollection<EncryptionKeyForIdentifier>(EncryptionKeyStorage.CollectionName), EncryptionKeyStorage.Indexes)
    ];

    /// <summary>
    /// Gets the package-owned indexes on the real jobs database.
    /// </summary>
    /// <param name="database">The jobs database, not the routed namespace database.</param>
    /// <returns>The index definitions.</returns>
    internal static IReadOnlyList<MongoDBIndexDefinition> ForJobs(IMongoDatabase database) =>
    [
        MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>(WellKnownCollectionNames.Jobs), MongoDBJobIndexes.Jobs),
        MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>(WellKnownCollectionNames.JobSteps), MongoDBJobIndexes.Steps),
        MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>(WellKnownCollectionNames.FailedJobSteps), MongoDBJobIndexes.Steps)
    ];

    /// <summary>
    /// Discovers the indexes in a namespace, including existing dynamic constraint collections.
    /// </summary>
    /// <param name="database">The namespace database.</param>
    /// <param name="constraints">The event store's current unique constraint definitions.</param>
    /// <param name="logger">The uniqueness fallback logger.</param>
    /// <param name="cancellationToken">Cancellation of discovery.</param>
    /// <returns>The bound index definitions.</returns>
    internal static async Task<MongoDBNamespaceIndexes> ForNamespace(IMongoDatabase database, IReadOnlyList<UniqueConstraintDefinition> constraints, ILogger<UniqueConstraintsStorage> logger, CancellationToken cancellationToken = default)
    {
        var definitions = new List<MongoDBIndexDefinition>
        {
            MongoDBIndexDefinition.For(database.GetCollection<AlertIncidentDocument>(WellKnownCollectionNames.AlertIncidents), AlertIncidentsStorage.Indexes),
            MongoDBIndexDefinition.For(database.GetCollection<FailedPartition>(WellKnownCollectionNames.FailedPartitions), FailedPartitionStorage.Indexes),
            MongoDBIndexDefinition.For(database.GetCollection<InFlightEvent>(WellKnownCollectionNames.InFlightEvents), InFlightEventsStorage.Indexes),
            MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>("occurrences"), OccurrencesStorage.Indexes)
        };
        definitions.AddRange(ForEncryption(database));
        definitions.Add(MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>("effect-receipts"), EventSequenceStorage.EffectReceiptIndexes));
        foreach (var collection in new[] { WellKnownCollectionNames.ObserverDefinitionChangeProgress, WellKnownCollectionNames.ObserverDefinitionChangeContinuations, WellKnownCollectionNames.ObserverDefinitionChangeRestartPlans, WellKnownCollectionNames.ObserverDefinitionChangeInventory })
        {
            definitions.Add(MongoDBIndexDefinition.For(database.GetCollection<BsonDocument>(collection), ObserverDefinitionChangeRows<BsonDocument>.Indexes));
        }

        using var sequences = await database.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).DistinctAsync<string>("_id", FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        var ids = (await sequences.ToListAsync(cancellationToken))
            .Concat([WellKnownCollectionNames.EventLog, WellKnownCollectionNames.System, WellKnownCollectionNames.Outbox]).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var id in ids)
        {
            definitions.Add(MongoDBIndexDefinition.For(database.GetCollection<Event>(id), EventStoreNamespaceDatabase.DesiredEventIndexes().Concat(EventSequenceStorage.SequenceIndexes), EventStoreNamespaceDatabase.ObsoleteEventIndexNames));
            definitions.Add(MongoDBIndexDefinition.For(database.GetCollection<ClosedStreamDocument>($"{id}+closed_streams"), ClosedStreamsConstraintStorage.ClosedStreamsIndexes));
        }
        var unclassified = await AddConstraints(database, definitions, constraints, ids, logger, cancellationToken);
        return new(definitions, unclassified);
    }

    static async Task<IReadOnlyList<string>> AddConstraints(IMongoDatabase database, List<MongoDBIndexDefinition> definitions, IReadOnlyList<UniqueConstraintDefinition> constraints, IReadOnlyList<string> sequences, ILogger<UniqueConstraintsStorage> logger, CancellationToken cancellationToken)
    {
        var unclassified = new List<string>();
        using var cursor = await database.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", new BsonRegularExpression(UniqueConstraintsStorage.CollectionNamePattern)) }, cancellationToken);
        foreach (var name in await cursor.ToListAsync(cancellationToken))
        {
            var mode = MongoDBConstraintCollectionClassifier.Classify(name, constraints, sequences);
            if (mode is null)
            {
                unclassified.Add(name);
            }
            else if (mode == UniqueConstraintMode.PerValue)
            {
                var collection = database.GetCollection<UniqueConstraintValueIndex>(name);
                definitions.Add(MongoDBIndexDefinition.For(
                    collection,
                    [UniqueConstraintsStorage.OwnerIndex, UniqueConstraintsStorage.SubjectIndex<UniqueConstraintValueIndex>(row => row.Subject)],
                    ensure: () => UniqueConstraintsStorage.EnsureOwnerIndexes(collection, cancellationToken)));
            }
            else
            {
                var collection = database.GetCollection<UniqueConstraintIndex>(name);
                definitions.Add(MongoDBIndexDefinition.For(
                    collection,
                    [UniqueConstraintsStorage.ValueIndex(true), UniqueConstraintsStorage.SubjectIndex<UniqueConstraintIndex>(row => row.Subject)],
                    ensure: () => UniqueConstraintsStorage.EnsureValueIndexes(collection, logger, cancellationToken)));
            }
        }
        return unclassified;
    }
}
