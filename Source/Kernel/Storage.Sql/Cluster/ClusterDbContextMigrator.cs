// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a startup task that runs migrations for the <see cref="ClusterDbContext"/> on startup.
/// </summary>
/// <remarks>
/// Goes through <see cref="IDatabase.Cluster"/>, so the cluster database is prepared exactly as on any other access:
/// created only when storage may create it, in the configured schema, and as the resolved schema identity.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/>.</param>
public class ClusterDbContextMigrator(IDatabase database) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await using var scope = await database.Cluster();
        if (database is Database { VerifySchema: true } sql)
        {
            await using var store = await database.EventStore(EventStoreName.System);
            await using var @namespace = await database.Namespace(EventStoreName.System, EventStoreNamespaceName.Default);
            await sql.PrepareJobs(EventStoreName.System, EventStoreNamespaceName.Default);
            await VerifyReadModels(scope.DbContext, sql);
        }
    }

    async Task VerifyReadModels(ClusterDbContext cluster, Database sql)
    {
        var stores = (await cluster.EventStores.Select(store => store.Name).ToListAsync())
            .Append(EventStoreName.System.Value).Distinct(StringComparer.Ordinal);
        foreach (var store in stores)
        {
            await using var context = await database.EventStore(store);
            var namespaces = (await context.DbContext.Namespaces.Select(ns => ns.Name).ToListAsync())
                .Append(EventStoreNamespaceName.Default.Value).Distinct(StringComparer.Ordinal);
            foreach (var ns in namespaces)
            {
                await sql.PrepareReadModels(new(DatabaseKind.Namespace, store, ns), true);
            }
        }
    }
}
