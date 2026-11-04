// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Represents a startup task that, when the schema is only verified, checks the read-model databases of every event store and
/// namespace, so a server whose read models are not current refuses to start.
/// </summary>
/// <remarks>
/// It opens every event store and namespace, so it runs after the names in use have been checked (see <see cref="Stage"/>), and
/// leaves the quarantined ones out: they refuse to be opened, and a failure there would stop the whole server. What it left out is
/// reported, and is verified on a later start, once the quarantine is resolved.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/>.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <param name="resourceNames">The optional <see cref="IResourceNameSnapshot"/> that tells which event stores and namespaces are quarantined.</param>
public class ReadModelSchemaVerification(
    IDatabase database,
    ILogger<ReadModelSchemaVerification> logger,
    IResourceNameSnapshot? resourceNames = null) : IStartupTask
{
    /// <summary>
    /// The silo lifecycle stage the verification runs in: right after <see cref="ServiceLifecycleStage.ApplicationServices"/>,
    /// where the names in use are checked and quarantined ones are published.
    /// </summary>
    public const int Stage = ServiceLifecycleStage.ApplicationServices + 1;

    /// <summary>
    /// Gets the event stores and namespaces the last run left out.
    /// </summary>
    public SkippedResources SkippedResources { get; private set; } = SkippedResources.None;

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        if (database is not Database { VerifySchema: true } sql)
        {
            return;
        }

        var namespaces = new Dictionary<EventStoreName, IEnumerable<EventStoreNamespaceName>>();
        async Task<IEnumerable<EventStoreNamespaceName>> NamespacesOf(EventStoreName store)
        {
            if (!namespaces.TryGetValue(store, out var names))
            {
                names = await ListNamespaces(store);
                namespaces[store] = names;
            }

            return names;
        }

        var eventStores = await UsableEventStores.From(await ListEventStores(), NamespacesOf, resourceNames, logger);
        SkippedResources = eventStores.SkippedResources;
        foreach (var store in eventStores.Usable)
        {
            foreach (var @namespace in await NamespacesOf(store))
            {
                if (!eventStores.SkippedResources.Namespaces.Contains((store, @namespace)))
                {
                    await sql.PrepareReadModels(new(DatabaseKind.Namespace, store.Value, @namespace.Value), true);
                }
            }
        }

        if (eventStores.HasSkipped)
        {
            logger.ReadModelsNotVerified(eventStores.SkippedResources.Describe());
        }
    }

    async Task<IEnumerable<EventStoreName>> ListEventStores()
    {
        await using var scope = await database.Cluster();
        var stores = await scope.DbContext.EventStores.Select(store => store.Name).ToListAsync();
        return stores.Append(EventStoreName.System.Value).Distinct(StringComparer.Ordinal).Select(name => (EventStoreName)name).ToArray();
    }

    async Task<IEnumerable<EventStoreNamespaceName>> ListNamespaces(EventStoreName store)
    {
        await using var context = await database.EventStore(store);
        var names = await context.DbContext.Namespaces.Select(ns => ns.Name).ToListAsync();
        return names.Append(EventStoreNamespaceName.Default.Value).Distinct(StringComparer.Ordinal).Select(name => (EventStoreNamespaceName)name).ToArray();
    }
}
