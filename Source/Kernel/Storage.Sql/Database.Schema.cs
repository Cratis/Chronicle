// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The schema-identity entry points used by the one-shot provisioner.
/// </summary>
public partial class Database
{
    /// <summary>
    /// Opens and prepares a scope using its schema identity.
    /// </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <param name="identity">The scope identity.</param>
    /// <param name="create">The context factory.</param>
    /// <param name="verify">Whether only read-only checks are permitted.</param>
    /// <returns>The prepared context.</returns>
    internal async Task<TContext> SchemaContext<TContext>(DatabaseIdentity identity, Func<DbContextOptions<TContext>, TContext> create, bool verify)
        where TContext : DbContext
    {
        var target = await Target(identity, ConnectionPurpose.Schema, verify);
        var context = create(BuildOptions<TContext>(target, verify ? SchemaMode.Verify : null));
        try
        {
            if (verify)
            {
                await VerifyDatabase(context, target);
                var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();
                if (pending.Length != 0)
                {
                    throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, identity.ToString(), pending);
                }
            }
            else
            {
                if (!CanCreateSchema && target.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite &&
                    !await context.Database.GetInfrastructure().GetRequiredService<IRelationalDatabaseCreator>().ExistsAsync())
                {
                    throw new DatabaseDoesNotExist(context.Database.GetDbConnection().Database);
                }

                await EnsureMigratedOnce($"schema:{typeof(TContext).Name}:{identity}:{target.ConnectionString}", target, context);
                if (identity == DatabaseIdentity.Cluster && UsesBindings)
                {
                    await _databaseNames.ReserveCluster((ClusterDbContext)(DbContext)context);
                }
            }

            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Prepares the jobs schema for a namespace.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The preparation task.</returns>
    internal async Task PrepareJobs(EventStoreName store, EventStoreNamespaceName @namespace)
    {
        await using var context = await SchemaContext<JobsDbContext>(new DatabaseIdentity(DatabaseKind.Namespace, store.Value, @namespace.Value), static opts => new JobsDbContext(opts), VerifySchema);
    }

    /// <summary>
    /// Prepares or verifies the read-model database without creating dynamic tables.
    /// </summary>
    /// <param name="identity">The namespace identity.</param>
    /// <param name="verify">Whether only read-only checks are permitted.</param>
    /// <returns>The database name.</returns>
    internal async Task<string> PrepareReadModels(DatabaseIdentity identity, bool verify)
    {
        var target = await Target(identity with { Kind = DatabaseKind.ReadModels }, ConnectionPurpose.Schema, verify);
        await using var context = new DbContext(BuildOptions<DbContext>(target, verify ? SchemaMode.Verify : null));
        if (verify)
        {
            await VerifyDatabase(context, target);
        }
        else if (target.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            await CreateDatabaseIfMissing(context, target, DatabaseType.Sqlite);
        }
        else
        {
            await EnsureDatabaseExists(context, target);
        }

        return context.Database.GetDbConnection().Database;
    }

    /// <summary>
    /// Prepares the known event-sequence tables.
    /// </summary>
    /// <param name="identity">The namespace identity.</param>
    /// <param name="names">The sequence names.</param>
    /// <param name="verify">Whether to inspect without executing DDL.</param>
    /// <returns>The preparation task.</returns>
    internal async Task PrepareSequences(DatabaseIdentity identity, IEnumerable<string> names, bool verify = false)
    {
        using var inspection = verify ? DynamicSchemaVerification.Begin() : null;
        var target = await Target(identity, ConnectionPurpose.Schema, verify);
        foreach (var name in names)
        {
            await using var context = new EventSequenceDbContext(BuildOptions<EventSequenceDbContext>(target), name, eventSequenceMigrator);
            if (verify) await context.EnsureTableExists();
            else await EnsureTableMigrated(context, target);
        }
    }

    async Task VerifyDatabase(DbContext context, Connections.SqlTarget target)
    {
        if (!await context.Database.GetInfrastructure().GetRequiredService<IRelationalDatabaseCreator>().ExistsAsync())
        {
            throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, context.GetType().Name, ["database missing"]);
        }

        try
        {
            await EnsureSchema(context, target, target.ConnectionString.GetDatabaseType(), true);
        }
        catch (SchemaDoesNotExist)
        {
            throw new SchemaNotCurrent(context.Database.GetDbConnection().Database, context.GetType().Name, ["schema missing"]);
        }
    }
}
