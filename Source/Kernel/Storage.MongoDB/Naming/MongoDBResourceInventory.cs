// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a read-only <see cref="IResourceInventory"/> for MongoDB.
/// </summary>
/// <remarks>
/// The event stores are the ones in the registry, the ones with a database on the server and always the System event store.
/// Only reads are made: no database is created, and nothing is written. A database is identified by where it is placed and its
/// name in upper case, which is a superset of the equality MongoDB applies. Unicode is never normalized, because MongoDB does not.
/// </remarks>
/// <param name="connections">The <see cref="IMongoDBStorageConnections"/>.</param>
/// <param name="clusterStorage">The <see cref="IClusterStorage"/> holding the registry of event stores.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="MongoDBStorageOptions"/>.</param>
public class MongoDBResourceInventory(
    IMongoDBStorageConnections connections,
    IClusterStorage clusterStorage,
    IOptions<MongoDBStorageOptions> options) : IResourceInventory
{
    static readonly string[] _serverDatabases = ["admin", "local", "config"];

    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceUse>> Read(CancellationToken cancellationToken)
    {
        var prefix = options.Value.DatabaseNamePrefix;
        var systemDatabaseName = options.Value.SystemDatabaseName;
        var uses = new List<ResourceUse>();

        var cluster = await connections.Placement(StorageConnectionKind.Cluster, DatabaseNames.ComposeClusterDatabase(prefix));
        var system = new ResourceOwner(EventStoreName.System.Value);
        uses.Add(Identify(null, ResourceKind.Kernel, cluster.Scope, cluster.Database));
        uses.Add(Identify(system, ResourceKind.EventStoreDatabase, cluster.Scope, systemDatabaseName));
        uses.Add(Identify(system, ResourceKind.EventStoreDatabase, cluster.Scope, DatabaseNames.LegacySystemDatabaseName));
        if (prefix.Length == 0)
        {
            uses.AddRange(_serverDatabases.Select(_ => Identify(null, ResourceKind.Kernel, cluster.Scope, _)));
        }

        foreach (var eventStore in await EventStores(prefix, systemDatabaseName, cancellationToken))
        {
            await AddEventStore(uses, eventStore, prefix, systemDatabaseName, cancellationToken);
        }

        return uses;
    }

    static ResourceUse Identify(ResourceOwner? owner, ResourceKind kind, string scope, string database) =>
        new(owner, kind, $"mongo:{scope}|{database.ToUpperInvariant()}");

    async Task<List<EventStoreName>> EventStores(string prefix, string systemDatabaseName, CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, EventStoreName>(StringComparer.Ordinal)
        {
            [EventStoreName.System.Value] = EventStoreName.System
        };

        foreach (var eventStore in await clusterStorage.GetEventStores())
        {
            names[eventStore.Value] = eventStore;
        }

        var cluster = connections.GetDatabase(StorageConnectionKind.Cluster, DatabaseNames.ComposeClusterDatabase(prefix));
        using var cursor = await cluster.Client.ListDatabaseNamesAsync(cancellationToken);
        foreach (var name in await cursor.ToListAsync(cancellationToken))
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith("+es", StringComparison.Ordinal))
            {
                continue;
            }

            var storeName = name[prefix.Length..^3];
            if (!string.IsNullOrWhiteSpace(storeName))
            {
                var eventStore = DatabaseNames.EventStoreFor(storeName, systemDatabaseName);
                names[eventStore.Value] = eventStore;
            }
        }

        return [.. names.Values];
    }

    async Task AddEventStore(List<ResourceUse> uses, EventStoreName eventStore, string prefix, string systemDatabaseName, CancellationToken cancellationToken)
    {
        var owner = new ResourceOwner(eventStore.Value);
        var eventStoreDatabase = DatabaseNames.ComposeForEventStore(eventStore, prefix, systemDatabaseName);
        var placement = await connections.Placement(StorageConnectionKind.EventStore, eventStoreDatabase, eventStore.Value);
        uses.Add(Identify(owner, ResourceKind.EventStoreDatabase, placement.Scope, placement.Database));

        foreach (var @namespace in await Namespaces(eventStore, eventStoreDatabase, cancellationToken))
        {
            var namespaceOwner = new ResourceOwner(eventStore.Value, @namespace.Value);
            var namespaceDatabase = DatabaseNames.ComposeForEventStoreNamespace(eventStore, @namespace, prefix, systemDatabaseName);
            placement = await connections.Placement(StorageConnectionKind.Namespace, namespaceDatabase, eventStore.Value, @namespace.Value);
            uses.Add(Identify(namespaceOwner, ResourceKind.NamespaceDatabase, placement.Scope, placement.Database));

            var readModelDatabase = DatabaseNames.ComposeForReadModels(eventStore, @namespace, prefix, systemDatabaseName);
            placement = await connections.Placement(StorageConnectionKind.ReadModels, readModelDatabase, eventStore.Value, @namespace.Value);
            uses.Add(Identify(namespaceOwner, ResourceKind.ReadModelDatabase, placement.Scope, placement.Database));
        }
    }

    async Task<List<EventStoreNamespaceName>> Namespaces(EventStoreName eventStore, string eventStoreDatabase, CancellationToken cancellationToken)
    {
        var collection = connections
            .GetDatabase(StorageConnectionKind.EventStore, eventStoreDatabase, eventStore.Value)
            .GetCollection<MongoDBNamespace>(WellKnownCollectionNames.Namespaces);
        using var cursor = await collection.FindAsync(_ => true, cancellationToken: cancellationToken);
        return [.. (await cursor.ToListAsync(cancellationToken)).Select(_ => new EventStoreNamespaceName(_.Name))];
    }
}
