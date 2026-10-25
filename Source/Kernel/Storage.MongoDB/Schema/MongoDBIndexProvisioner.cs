// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Provisions every known kernel-owned MongoDB index without starting Orleans or HTTP listeners.
/// </summary>
/// <param name="database">The routed Chronicle database.</param>
/// <param name="clients">The client manager used by jobs.</param>
/// <param name="mongoOptions">The configured jobs server and cluster database.</param>
/// <param name="storageOptions">The database prefix.</param>
/// <param name="loggerFactory">The logger factory.</param>
public class MongoDBIndexProvisioner(
    IDatabase database,
    IMongoDBClientManager clients,
    IOptions<MongoDBOptions> mongoOptions,
    IOptions<MongoDBStorageOptions> storageOptions,
    ILoggerFactory loggerFactory) : IMongoDBIndexProvisioner
{
    readonly ILogger<MongoDBIndexProvisioner> _logger = loggerFactory.CreateLogger<MongoDBIndexProvisioner>();

    /// <inheritdoc/>
    public Task<MongoDBIndexReport> Verify(IEnumerable<string>? eventStores = null, IEnumerable<string>? namespaces = null, CancellationToken cancellationToken = default) =>
        Run(eventStores ?? [], namespaces ?? [], false, false, cancellationToken);

    /// <inheritdoc/>
    public Task<MongoDBIndexReport> Migrate(IEnumerable<string>? eventStores = null, IEnumerable<string>? namespaces = null, CancellationToken cancellationToken = default) =>
        Run(eventStores ?? [], namespaces ?? [], true, false, cancellationToken);

    /// <summary>
    /// Verifies only System/Default, its event-store level and jobs, and global encryption without store or namespace discovery.
    /// </summary>
    /// <param name="cancellationToken">Cancellation of the inspection.</param>
    /// <returns>The fixed startup scopes' index state.</returns>
    internal Task<MongoDBIndexReport> VerifyStartup(CancellationToken cancellationToken = default) =>
        Run([], [], false, true, cancellationToken);

    static async Task<string[]> NamespaceNames(IMongoCollection<MongoDBNamespace> collection, string[] requested, bool startupOnly, CancellationToken cancellationToken)
    {
        if (startupOnly) return [EventStoreNamespaceName.Default.Value];
        using var cursor = await collection.FindAsync(FilterDefinition<MongoDBNamespace>.Empty, cancellationToken: cancellationToken);
        return (await cursor.ToListAsync(cancellationToken)).Select(row => row.Name)
            .Concat(requested).Append(EventStoreNamespaceName.Default.Value).Distinct(StringComparer.Ordinal).ToArray();
    }

    async Task<MongoDBIndexReport> Run(IEnumerable<string> eventStores, IEnumerable<string> namespaces, bool migrate, bool startupOnly, CancellationToken cancellationToken)
    {
        var requestedStores = eventStores.ToArray();
        var requestedNamespaces = namespaces.ToArray();
        var results = new List<MongoDBIndexScopeResult>();
        foreach (var store in await StoreNames(requestedStores, startupOnly, cancellationToken))
        {
            var storeDatabase = database.GetEventStoreDatabase(store);
            var namespaceCollection = storeDatabase.GetCollection<MongoDBNamespace>(WellKnownCollectionNames.Namespaces);
            await Visit($"event-store:{store}", namespaceCollection.Database, MongoDBIndexCatalogue.ForEventStore(namespaceCollection.Database), migrate, results, [], cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var constraints = (await new ConstraintsStorage(storeDatabase).GetDefinitions()).OfType<UniqueConstraintDefinition>().ToArray();
            var extra = requestedStores.Contains(store, StringComparer.Ordinal) ? requestedNamespaces : [];
            foreach (var name in await NamespaceNames(namespaceCollection, extra, startupOnly, cancellationToken))
            {
                var namespaceDatabase = storeDatabase.GetNamespaceDatabase(name).GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database;
                var indexes = await MongoDBIndexCatalogue.ForNamespace(namespaceDatabase, constraints, loggerFactory.CreateLogger<UniqueConstraintsStorage>(), cancellationToken);
                await Visit($"namespace:{store}/{name}", namespaceDatabase, indexes.Definitions, migrate, results, indexes.Unclassified, cancellationToken);

                // The shared Orleans jobs adapter deliberately uses the configured server, not namespace routing.
                var jobs = clients.GetClientFor(MongoDBChronicleBuilderExtensions.GetMongoClientSettings(mongoOptions.Value.Server))
                    .GetDatabase(DatabaseNames.ForEventStoreNamespace(store, name, storageOptions.Value.DatabaseNamePrefix));
                await Visit($"jobs:{store}/{name}", jobs, MongoDBIndexCatalogue.ForJobs(jobs), migrate, results, [], cancellationToken);
            }
        }
        var encryption = database.GetEventStoreDatabase(EncryptedValueKeyIdentifiers.GlobalEventStore).GetNamespaceDatabase(EncryptedValueKeyIdentifiers.GlobalNamespace)
            .GetCollection<BsonDocument>(EncryptionKeyStorage.CollectionName).Database;
        await Visit("global-encryption", encryption, MongoDBIndexCatalogue.ForEncryption(encryption), migrate, results, [], cancellationToken);
        return new(results);
    }

    async Task<string[]> StoreNames(string[] requested, bool startupOnly, CancellationToken cancellationToken)
    {
        if (startupOnly) return [EventStoreName.System.Value];
        using var cursor = await database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores).FindAsync(FilterDefinition<EventStore>.Empty, cancellationToken: cancellationToken);
        return (await cursor.ToListAsync(cancellationToken)).Select(store => store.Name.Value)
            .Concat(requested).Append(EventStoreName.System.Value).Distinct(StringComparer.Ordinal).ToArray();
    }

    async Task Visit(string scope, IMongoDatabase mongo, IReadOnlyList<MongoDBIndexDefinition> definitions, bool migrate, List<MongoDBIndexScopeResult> results, IReadOnlyList<string> unclassified, CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        var obsolete = new List<string>();
        var created = 0;
        var dropped = 0;
        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await definition.Existing();
            var pending = definition.Names.Except(existing, StringComparer.Ordinal).ToArray();
            var old = definition.Obsolete.Where(existing.Contains).ToArray();
            if (migrate)
            {
                foreach (var name in old) await definition.Drop(name);
                await definition.Ensure();
                created += pending.Length;
                dropped += old.Length;
                existing = await definition.Existing();
            }
            missing.AddRange(definition.Names.Except(existing, StringComparer.Ordinal).Select(name => $"{definition.Collection}/{name}"));
            obsolete.AddRange(definition.Obsolete.Where(existing.Contains).Select(name => $"{definition.Collection}/{name}"));
        }
        results.Add(new(scope, mongo.DatabaseNamespace.DatabaseName, missing, obsolete) { Unclassified = unclassified });
        _logger.IndexState(scope, mongo.DatabaseNamespace.DatabaseName, missing.Count, obsolete.Count, created, dropped);
        if (missing.Count > 0 || obsolete.Count > 0)
        {
            _logger.IndexesMissing(scope, mongo.DatabaseNamespace.DatabaseName, string.Join(", ", missing), string.Join(", ", obsolete));
        }
        if (unclassified.Count > 0)
        {
            var action = migrate
                ? "No index DDL was performed on these collections. Runtime will create their indexes on first access."
                : "These collections were not verified and do not make the report non-current.";
            _logger.UnclassifiedConstraints(scope, mongo.DatabaseNamespace.DatabaseName, string.Join(", ", unclassified), action);
        }
    }
}
