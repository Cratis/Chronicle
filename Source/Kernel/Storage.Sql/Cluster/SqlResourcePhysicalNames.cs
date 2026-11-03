// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents the SQL implementation of <see cref="IResourcePhysicalNames"/>, using the permanent database name bindings.
/// </summary>
/// <remarks>
/// Only providers that give each event store and namespace its own database have names to protect. For the others no names are returned.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/>.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
public class SqlResourcePhysicalNames(IDatabase database, IOptions<ChronicleOptions> options) : IResourcePhysicalNames
{
    bool UsesBindings => options.Value.Storage.ConnectionDetails.GetDatabaseType() is DatabaseType.PostgreSql or DatabaseType.SqlServer;

    /// <inheritdoc/>
    public IReadOnlyCollection<string> For(EventStoreName eventStore, EventStoreNamespaceName? @namespace)
    {
        if (!UsesBindings)
        {
            return [];
        }

        var cluster = new SqlDatabaseCatalog(options.Value.Storage.ConnectionDetails).ClusterName;
        if (@namespace is null)
        {
            return [DatabaseIdentity.Store(eventStore.Value).LogicalName(cluster)];
        }

        return
        [
            new DatabaseIdentity(DatabaseKind.Namespace, eventStore.Value, @namespace.Value).LogicalName(cluster),
            new DatabaseIdentity(DatabaseKind.ReadModels, eventStore.Value, @namespace.Value).LogicalName(cluster)
        ];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Compared on the server, one name at a time, so the bindings column collation decides: the server collation on
    /// SQL Server (case, accent, width, kana and trailing spaces as the server compares database names) and "C" on PostgreSQL.
    /// </remarks>
    public async Task<bool> AnyExists(IReadOnlyCollection<string> names)
    {
        if (!UsesBindings || names.Count == 0)
        {
            return false;
        }

        await using var scope = await database.Cluster();
        foreach (var name in names)
        {
            if (await scope.DbContext.DatabaseNames.AsNoTracking().AnyAsync(row => row.PhysicalName == name))
            {
                return true;
            }
        }

        return false;
    }
}
