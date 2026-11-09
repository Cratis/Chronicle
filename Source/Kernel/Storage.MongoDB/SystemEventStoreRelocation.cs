// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
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

        var system = EventStoreName.System;
        var server = clientManager.GetClientFor(Setup.MongoDBChronicleBuilderExtensions.GetMongoClientSettings(mongoDBOptions.Value.Server));

        // Hosted MongoDB offerings, such as Atlas Flex, own a database named System that the user cannot read, and a fresh
        // installation never had one of its own. With no database of the System event store under the old name, there is
        // nothing to move, and nothing to be authorized for.
        if (!await AnyDatabaseExists(server, DatabaseNames.ForEventStore(system, prefix, from)))
        {
            logger.NothingToMoveFrom(prefix, from);
            return;
        }

        logger.Moving(prefix, from, to);
        var eventStore = connections.GetDatabase(StorageConnectionKind.EventStore, DatabaseNames.ForEventStore(system, prefix, from), system.Value);

        var outcomes = new List<DatabaseMoveOutcome>();
        foreach (var @namespace in await Namespaces(eventStore, prefix, from))
        {
            outcomes.Add(await Move(StorageConnectionKind.Namespace, DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, from), DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, to), @namespace));
            outcomes.Add(await Move(StorageConnectionKind.ReadModels, DatabaseNames.ForReadModels(system, @namespace, prefix, from), DatabaseNames.ForReadModels(system, @namespace, prefix, to), @namespace));
            outcomes.Add(await Move(
                server.GetDatabase(DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, from)),
                server.GetDatabase(DatabaseNames.ForEventStoreNamespace(system, @namespace, prefix, to))));
        }

        outcomes.Add(await Move(StorageConnectionKind.EventStore, DatabaseNames.ForEventStore(system, prefix, from), DatabaseNames.ForEventStore(system, prefix, to), EventStoreNamespaceName.Default, includeNamespace: false));
        if (outcomes.Contains(DatabaseMoveOutcome.Kept))
        {
            return;
        }

        if (outcomes.Contains(DatabaseMoveOutcome.MovedSourceRetained))
        {
            logger.MovedWithSourcesRetained(from, to);
            return;
        }

        logger.Moved(from, to);
    }

    /// <inheritdoc/>
    public async Task<string?> FindRetainedLegacyData()
    {
        var prefix = storageOptions.Value.DatabaseNamePrefix;
        var configured = storageOptions.Value.SystemDatabaseName;
        if (string.Equals(configured, DatabaseNames.LegacySystemDatabaseName, StringComparison.OrdinalIgnoreCase) ||
            !await HoldsEvents(prefix, configured))
        {
            return null;
        }

        var server = clientManager.GetClientFor(Setup.MongoDBChronicleBuilderExtensions.GetMongoClientSettings(mongoDBOptions.Value.Server));
        var legacy = DatabaseNames.ForReadModels(EventStoreName.System, EventStoreNamespaceName.Default, prefix, DatabaseNames.LegacySystemDatabaseName);
        var retained = (await (await server.ListDatabaseNamesAsync()).ToListAsync())
            .Where(_ => _ == legacy || _.StartsWith($"{legacy}+", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (retained.Length == 0)
        {
            return null;
        }

        return $"The System event store is kept in the databases named '{prefix}{configured}', but databases from before Chronicle 19.117 remain under '{legacy}': {string.Join(", ", retained.Select(_ => $"'{_}'"))}. " +
            "Their content was moved and they are stale copies the kernel does not read, left behind because the MongoDB user lacks the dropDatabase and dropCollection actions on them. " +
            $"Drop them with a user that has those actions, and do not set storage.systemDatabaseName back to '{DatabaseNames.LegacySystemDatabaseName}', which would start the kernel against the stale copy.";
    }

    /// <inheritdoc/>
    public async Task<string?> FindMisplacedData()
    {
        var prefix = storageOptions.Value.DatabaseNamePrefix;
        var configured = storageOptions.Value.SystemDatabaseName;
        var other = string.Equals(configured, DatabaseNames.LegacySystemDatabaseName, StringComparison.OrdinalIgnoreCase)
            ? Configuration.Storage.DefaultSystemDatabaseName
            : DatabaseNames.LegacySystemDatabaseName;

        if (await HoldsEvents(prefix, configured) || !await HoldsEvents(prefix, other))
        {
            return null;
        }

        return $"The System event store is empty under the configured database name '{configured}', but holds events under '{other}'. " +
            $"The databases named '{DatabaseNames.LegacySystemDatabaseName}' were renamed to '{Configuration.Storage.DefaultSystemDatabaseName}' in Chronicle 19.117. " +
            $"Set storage.systemDatabaseName to '{other}' (Cratis__Chronicle__Storage__SystemDatabaseName) or move the data back; the kernel does not copy it back.";
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

    static async Task<bool> AnyDatabaseExists(IMongoClient server, string eventStoreDatabaseName)
    {
        var names = await (await server.ListDatabaseNamesAsync()).ToListAsync();
        return names.Exists(_ =>
            string.Equals(_, eventStoreDatabaseName, StringComparison.OrdinalIgnoreCase) ||
            _.StartsWith($"{eventStoreDatabaseName}+", StringComparison.OrdinalIgnoreCase));
    }

    async Task<bool> HoldsEvents(string prefix, string systemDatabaseName)
    {
        var server = clientManager.GetClientFor(Setup.MongoDBChronicleBuilderExtensions.GetMongoClientSettings(mongoDBOptions.Value.Server));
        var databaseName = DatabaseNames.ForEventStoreNamespace(EventStoreName.System, EventStoreNamespaceName.Default, prefix, systemDatabaseName);
        var names = await (await server.ListDatabaseNamesAsync()).ToListAsync();
        if (!names.Contains(databaseName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var events = server.GetDatabase(databaseName).GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog);
        return await events.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).AnyAsync();
    }

    Task<DatabaseMoveOutcome> Move(StorageConnectionKind kind, string fromName, string toName, EventStoreNamespaceName @namespace, bool includeNamespace = true) =>
        Move(
            connections.GetDatabase(kind, fromName, EventStoreName.System.Value, includeNamespace ? @namespace.Value : string.Empty),
            connections.GetDatabase(kind, toName, EventStoreName.System.Value, includeNamespace ? @namespace.Value : string.Empty));

    async Task<DatabaseMoveOutcome> Move(IMongoDatabase source, IMongoDatabase target)
    {
        var sourceName = source.DatabaseNamespace.DatabaseName;
        var targetName = target.DatabaseNamespace.DatabaseName;

        // Connection resolution can name a database explicitly, in which case both names resolve to the same database.
        // Copying it onto itself and dropping the source would delete it.
        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase))
        {
            logger.DatabaseNameUnchanged(sourceName);
            return DatabaseMoveOutcome.NothingToMove;
        }

        var outcome = await MongoDatabaseMove.Move(source, target, logger);
        switch (outcome)
        {
            case DatabaseMoveOutcome.Moved:
                logger.DatabaseMoved(sourceName, targetName);
                break;

            case DatabaseMoveOutcome.MovedSourceRetained:
                logger.DatabaseMovedSourceRetained(sourceName, targetName);
                break;

            case DatabaseMoveOutcome.Kept:
                logger.DatabaseKept(sourceName, targetName);
                break;
        }

        return outcome;
    }
}
