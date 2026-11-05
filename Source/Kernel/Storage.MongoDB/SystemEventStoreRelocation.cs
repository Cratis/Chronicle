// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISystemEventStoreRelocation"/>.
/// </summary>
/// <remarks>
/// Every database the System event store has is moved: the event store database, and for each namespace the
/// namespace database and the read model database. The jobs of a namespace live in a database with the namespace
/// database's name on the configured server even when connection resolution places the namespace database elsewhere,
/// so that is moved too. A database whose name does not change, because connection resolution names it explicitly,
/// is left where it is.
/// </remarks>
/// <param name="connections">The <see cref="IMongoDBStorageConnections"/> that resolves where each database is.</param>
/// <param name="clientManager">The <see cref="IMongoDBClientManager"/> for the configured server.</param>
/// <param name="mongoDBOptions">The <see cref="MongoDBOptions"/> naming the configured server.</param>
/// <param name="storageOptions">The <see cref="MongoDBStorageOptions"/> naming the databases.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public class SystemEventStoreRelocation(
    IMongoDBStorageConnections connections,
    IMongoDBClientManager clientManager,
    IOptions<MongoDBOptions> mongoDBOptions,
    IOptions<MongoDBStorageOptions> storageOptions,
    ILogger<SystemEventStoreRelocation> logger) : ISystemEventStoreRelocation
{
    /// <inheritdoc/>
    public async Task MoveTo(SystemEventStoreLocation location)
    {
        var prefix = storageOptions.Value.DatabaseNamePrefix;
        var configured = storageOptions.Value.SystemDatabaseName;
        var (from, to) = location == SystemEventStoreLocation.Configured
            ? (DatabaseNames.LegacySystemDatabaseName, configured)
            : (configured, DatabaseNames.LegacySystemDatabaseName);

        // MongoDB compares database names without regard to case, so names differing only by case are the same database.
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            logger.NothingToMove(to);
            return;
        }

        logger.Moving(prefix, from, to);
        var system = EventStoreName.System;
        var eventStore = connections.GetDatabase(StorageConnectionKind.EventStore, DatabaseNames.ForEventStore(system, prefix, from), system.Value);
        var server = clientManager.GetClientFor(Setup.MongoDBChronicleBuilderExtensions.GetMongoClientSettings(mongoDBOptions.Value.Server));

        var complete = true;
        foreach (var @namespace in await Namespaces(eventStore, prefix, from))
        {
            complete &= await Move(StorageConnectionKind.Namespace, DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, from), DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, to), @namespace);
            complete &= await Move(StorageConnectionKind.ReadModels, DatabaseNames.ForReadModels(system, @namespace, prefix, from), DatabaseNames.ForReadModels(system, @namespace, prefix, to), @namespace);
            complete &= await Move(
                server.GetDatabase(DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, from)),
                server.GetDatabase(DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, to)));
        }

        complete &= await Move(StorageConnectionKind.EventStore, DatabaseNames.ForEventStore(system, prefix, from), DatabaseNames.ForEventStore(system, prefix, to), EventStoreNamespaceName.Default, includeNamespace: false);
        if (complete)
        {
            logger.Moved(from, to);
        }
    }

    static async Task<IEnumerable<EventStoreNamespaceName>> Namespaces(IMongoDatabase eventStore, string prefix, string from)
    {
        var registered = await (await eventStore.GetCollection<MongoDBNamespace>(WellKnownCollectionNames.Namespaces)
            .FindAsync(FilterDefinition<MongoDBNamespace>.Empty)).ToListAsync();

        // A namespace database can exist without a registration, for instance when it was created before the
        // namespace was registered, so the database names on the server are consulted as well.
        var namespacePrefix = DatabaseNames.ForEventStoreNamespace(EventStoreName.System, "x", prefix, from)[..^1];
        var discovered = await (await eventStore.Client.ListDatabaseNamesAsync()).ToListAsync();

        return registered.Select(_ => _.Name)
            .Concat(discovered.Where(_ => _.StartsWith(namespacePrefix, StringComparison.Ordinal)).Select(_ => _[namespacePrefix.Length..]))
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Append(EventStoreNamespaceName.Default.Value)
            .Distinct(StringComparer.Ordinal)
            .Select(_ => new EventStoreNamespaceName(_))
            .ToArray();
    }

    Task<bool> Move(StorageConnectionKind kind, string fromName, string toName, EventStoreNamespaceName @namespace, bool includeNamespace = true) =>
        Move(
            connections.GetDatabase(kind, fromName, EventStoreName.System.Value, includeNamespace ? @namespace.Value : string.Empty),
            connections.GetDatabase(kind, toName, EventStoreName.System.Value, includeNamespace ? @namespace.Value : string.Empty));

    async Task<bool> Move(IMongoDatabase source, IMongoDatabase target)
    {
        var sourceName = source.DatabaseNamespace.DatabaseName;
        var targetName = target.DatabaseNamespace.DatabaseName;

        // Connection resolution can name a database explicitly, in which case both names resolve to the same database.
        // Copying it onto itself and dropping the source would delete it.
        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase))
        {
            logger.DatabaseNameUnchanged(sourceName);
            return true;
        }

        switch (await MongoDatabaseMove.Move(source, target, logger))
        {
            case DatabaseMoveOutcome.Moved:
                logger.DatabaseMoved(sourceName, targetName);
                break;

            case DatabaseMoveOutcome.Kept:
                logger.DatabaseKept(sourceName, targetName);
                return false;
        }

        return true;
    }
}
