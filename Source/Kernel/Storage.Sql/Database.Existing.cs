// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that reaches storage that already exists, without ever provisioning anything.
/// </summary>
/// <remarks>
/// Nothing here creates a database file, a database, a schema, a table, an index, a migration history or a
/// <c language="csharp">DatabaseNames</c> binding. Storage that is missing is reported as absent.
/// </remarks>
public partial class Database
{
    /// <summary>
    /// SQLITE_CANTOPEN: the database file does not exist and the connection may not create it.
    /// </summary>
    const int SqliteCannotOpen = 14;

    /// <summary>
    /// PostgreSQL invalid_catalog_name: the database does not exist.
    /// </summary>
    const string PostgreSqlMissingDatabase = "3D000";

    /// <summary>
    /// PostgreSQL undefined_table and invalid_schema_name: the cluster database was never migrated.
    /// </summary>
    static readonly string[] _postgreSqlMissingObjects = ["42P01", "3F000"];

    /// <summary>
    /// SQL Server: cannot open the database requested by the login (4060), or invalid object name (208).
    /// </summary>
    static readonly int[] _sqlServerMissingStorage = [4060, 208];

    static bool IsMissingStorage(Exception exception) => exception switch
    {
        SqliteException sqlite => sqlite.SqliteErrorCode == SqliteCannotOpen,
        PostgresException postgres => postgres.SqlState == PostgreSqlMissingDatabase || _postgreSqlMissingObjects.Contains(postgres.SqlState),
        SqlException sqlServer => _sqlServerMissingStorage.Contains(sqlServer.Number),
        _ => false
    };

    static SqlTarget WithoutCreate(SqlTarget target)
    {
        if (target.ConnectionString.GetDatabaseType() != DatabaseType.Sqlite)
        {
            return target;
        }

        var builder = new SqliteConnectionStringBuilder(target.ConnectionString);
        if (builder.Mode == SqliteOpenMode.Memory || builder.DataSource.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return target;
        }

        builder.Mode = SqliteOpenMode.ReadWrite;
        return target with { ConnectionString = builder.ConnectionString };
    }

    /// <summary>
    /// Resolve where an already provisioned database is, without creating its binding.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>The <see cref="SqlTarget"/>, or null when the database was never bound or cannot be reached.</returns>
    async Task<SqlTarget?> ExistingTarget(DatabaseIdentity identity, ConnectionPurpose purpose)
    {
        try
        {
            return WithoutCreate(await Target(identity, purpose, readOnly: true));
        }
        catch (Exception exception) when (exception is SchemaNotCurrent or DatabaseNameBindingFailure || IsMissingStorage(exception))
        {
            return null;
        }
    }

    /// <summary>
    /// Create a database context for data that is only read if it is already there.
    /// </summary>
    /// <typeparam name="TDbContext">The type of database context.</typeparam>
    /// <param name="request">The identity, cache key, options cache, context factory and the probe that tells whether what the context needs is in place.</param>
    /// <returns>The context, or null when the database or what the probe looks for does not exist. The caller owns it.</returns>
    async Task<TDbContext?> CreateExistingContext<TDbContext>(ExistingContextRequest<TDbContext> request)
        where TDbContext : DbContext
    {
        var target = await ExistingTarget(request.Identity, ConnectionPurpose.Runtime);
        if (target is null)
        {
            return null;
        }

        var context = request.Create(OptionsFor(request.Cache, $"{request.Key}:{nameof(ConnectionPurpose.Runtime)}", target));
        try
        {
            if (await ExistsFor(request, context, target))
            {
                return context;
            }
        }
        catch (Exception exception) when (IsMissingStorage(exception))
        {
            // The database is not there, and nothing here creates it.
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }

        await context.DisposeAsync();
        return null;
    }

    async Task<bool> ExistsFor<TDbContext>(ExistingContextRequest<TDbContext> request, TDbContext context, SqlTarget target)
        where TDbContext : DbContext
    {
        if (!_resolution.IsConfigured)
        {
            return await request.Exists(context, target);
        }

        // With connection resolution configured the probe runs on the schema identity, as preparation does.
        var schemaTarget = await ExistingTarget(request.Identity, ConnectionPurpose.Schema);
        if (schemaTarget is null)
        {
            return false;
        }

        await using var schemaContext = request.Create(OptionsFor(request.Cache, $"{request.Key}:{nameof(ConnectionPurpose.Schema)}", schemaTarget));
        return await request.Exists(schemaContext, schemaTarget);
    }

    async Task<DbContextScope<ReadModelDbContext>?> GetExistingReadModelDbContext(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        Func<DbContextOptions<ReadModelDbContext>, string, ReadModelDbContext> createDbContext)
    {
#pragma warning disable CA2000
        var dbContext = await CreateExistingContext(new ExistingContextRequest<ReadModelDbContext>(
            new(DatabaseKind.ReadModels, eventStore.Value, @namespace.Value),
            $"readmodel-read:{eventStore.Value}:{@namespace.Value}",
            _readModelOptions,
            options => createDbContext(options, tableName),
            (context, _) => readModelMigrator.ProbeExistingTable(context.TableName, context)));
#pragma warning restore CA2000

        return dbContext is null ? null : new DbContextScope<ReadModelDbContext>(dbContext, static () => { });
    }

    sealed record ExistingContextRequest<TDbContext>(
        DatabaseIdentity Identity,
        string Key,
        ConcurrentDictionary<string, CachedDbContextOptions<TDbContext>> Cache,
        Func<DbContextOptions<TDbContext>, TDbContext> Create,
        Func<TDbContext, SqlTarget, Task<bool>> Exists)
        where TDbContext : DbContext;
}
