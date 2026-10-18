// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that creates databases and schemas and keeps their tables migrated.
/// </summary>
public partial class Database
{
    static async Task Execute(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The only interpolated value is the configured schema name, quoted as an identifier.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    static async Task<long> Scalar(System.Data.Common.DbConnection connection, string sql, string schema)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Constant SQL; the schema is a parameter.
        command.CommandText = sql;
#pragma warning restore CA2100
        var parameter = command.CreateParameter();
        parameter.ParameterName = "schema";
        parameter.Value = schema;
        command.Parameters.Add(parameter);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    Task EnsureMigratedOnce(string key, SqlTarget target, DbContext context) => MigrateWithLock(context, target, key);

    void RefreshMigrationCaches(DatabaseMigrationLock migrationLock)
    {
        if (_migrationGenerations.GetValueOrDefault(migrationLock) == migrationLock.Generation)
        {
            return;
        }

        // Another Database instance may have reset this file. Its local cache clear cannot invalidate ours.
        ClearTableMigrationCache(string.Empty);
        _migrationGenerations[migrationLock] = migrationLock.Generation;
    }

    /// <summary>
    /// Serializes EF Core <c language="csharp">MigrateAsync</c> calls per physical target to prevent concurrent migration race conditions
    /// when multiple grains access the same database simultaneously.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to migrate.</param>
    /// <param name="target">The <see cref="SqlTarget"/> the context connects to.</param>
    /// <param name="key">The migration cache key for this context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="DatabaseMigrationIncomplete">A duplicate object left migrations pending.</exception>
    async Task MigrateWithLock(DbContext context, SqlTarget target, string key)
    {
        // For SQLite the file is created automatically when EF Core opens a connection
        // to a non-existent path. PostgreSQL and SQL Server require the database to
        // exist before a connection succeeds, so explicitly create it first when the
        // event-store / namespace targets a per-database isolation name that has not
        // been provisioned yet. This mirrors MongoDB, where each event store and each
        // namespace lives in its own database that the kernel creates on first access.
        await EnsureDatabaseExists(context, target);

        var migrationLock = DatabaseMigrationLock.For(target.ConnectionString);
        await migrationLock.Semaphore.WaitAsync();
        try
        {
            RefreshMigrationCaches(migrationLock);
            if (_migratedKeys.ContainsKey(key))
            {
                return;
            }

            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex) when (IsAlreadyExistsException(ex))
            {
                var pending = (await context.Database.GetPendingMigrationsAsync()).FirstOrDefault();
                if (pending is not null)
                {
                    throw new DatabaseMigrationIncomplete(context.Database.GetDbConnection().Database, pending, ex);
                }

                // Only a complete migration history proves the concurrent migration succeeded.
            }

            _migratedKeys.TryAdd(key, true);
        }
        finally
        {
            migrationLock.Semaphore.Release();
        }
    }

    /// <summary>
    /// Make sure the database and schema a context connects to exist.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/>.</param>
    /// <param name="target">The <see cref="SqlTarget"/> it connects to.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DatabaseDoesNotExist">The database is missing and storage may not create it.</exception>
    async Task EnsureDatabaseExists(DbContext context, SqlTarget target)
    {
        var connectionString = target.ConnectionString;
        var databaseType = connectionString.GetDatabaseType();
        if (databaseType is not DatabaseType.PostgreSql and not DatabaseType.SqlServer)
        {
            return;
        }

        // Hot-path fast-skip: once a database has been confirmed to exist for this connection
        // string we never roundtrip again. The kernel calls EnsureDatabaseExists on every read
        // model / table request, so without this cache PostgreSQL/SQL Server would issue an
        // ExistsAsync probe per operation — a cumulative slowdown that visibly dominates the
        // ResetKernelState window on integration test boundaries.
        if (!_ensuredDatabaseKeys.ContainsKey(connectionString))
        {
            await CreateDatabaseIfMissing(context, target, databaseType);
        }

        await EnsureSchema(context, target, databaseType);
    }

    async Task CreateDatabaseIfMissing(DbContext context, SqlTarget target, DatabaseType databaseType)
    {
        var connectionString = target.ConnectionString;
        var migrationLock = DatabaseMigrationLock.For(connectionString).Semaphore;
        await migrationLock.WaitAsync();
        try
        {
            if (_ensuredDatabaseKeys.ContainsKey(connectionString))
            {
                return;
            }

            var creator = context.Database.GetInfrastructure().GetRequiredService<IRelationalDatabaseCreator>();
            if (!await creator.ExistsAsync())
            {
                if (!options.Value.Storage.CreateIfMissing)
                {
                    throw new DatabaseDoesNotExist(context.Database.GetDbConnection().Database);
                }

                try
                {
                    if (databaseType == DatabaseType.SqlServer && context.Database.GetDbConnection().Database.Length > 124)
                    {
                        // SQL Server's default logical log filename appends _log to the database name.
                        // Explicit file names keep that second sysname within 128 units too.
                        await SqlServerDatabaseCreation.Create(connectionString, _resolution.PrepareRawConnection);
                    }
                    else
                    {
                        await creator.CreateAsync();
                    }
                }
                catch (Exception ex) when (IsAlreadyExistsException(ex))
                {
                    // Another concurrent migrator created the database; safe to ignore.
                }
            }

            _ensuredDatabaseKeys.TryAdd(connectionString, true);
        }
        finally
        {
            migrationLock.Release();
        }
    }

    /// <summary>
    /// Make sure the schema a context keeps its tables in exists, and that the database user lands in it.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/>.</param>
    /// <param name="target">The <see cref="SqlTarget"/> it connects to.</param>
    /// <param name="databaseType">The <see cref="DatabaseType"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="SchemaDoesNotExist">The schema is missing and storage may not create it.</exception>
    /// <exception cref="DefaultSchemaMismatch">The SQL Server user does not have the schema as its default schema.</exception>
    async Task EnsureSchema(DbContext context, SqlTarget target, DatabaseType databaseType)
    {
        var key = $"{target.ConnectionString}|{target.Schema}";
        if (string.IsNullOrEmpty(target.Schema) || _ensuredSchemaKeys.ContainsKey(key))
        {
            return;
        }

        var database = context.Database.GetDbConnection().Database;
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        try
        {
            if (databaseType == DatabaseType.PostgreSql)
            {
                await EnsurePostgreSqlSchema(connection, database, target.Schema);
            }
            else
            {
                await EnsureSqlServerSchema(connection, database, target.Schema);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        _ensuredSchemaKeys.TryAdd(key, true);
    }

    async Task EnsurePostgreSqlSchema(System.Data.Common.DbConnection connection, string database, string schema)
    {
        if (options.Value.Storage.CreateIfMissing)
        {
            await Execute(connection, $"CREATE SCHEMA IF NOT EXISTS \"{schema.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
            return;
        }

        if (await Scalar(connection, "SELECT COUNT(*) FROM pg_namespace WHERE nspname = @schema", schema) == 0)
        {
            throw new SchemaDoesNotExist(database, schema);
        }
    }

    async Task EnsureSqlServerSchema(System.Data.Common.DbConnection connection, string database, string schema)
    {
        if (await Scalar(connection, "SELECT COUNT(*) FROM sys.schemas WHERE name = @schema", schema) == 0)
        {
            if (!options.Value.Storage.CreateIfMissing)
            {
                throw new SchemaDoesNotExist(database, schema);
            }

            await Execute(connection, $"CREATE SCHEMA [{schema.Replace("]", "]]", StringComparison.Ordinal)}]");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SCHEMA_NAME()";
        var actual = Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
        if (!string.Equals(actual, schema, StringComparison.OrdinalIgnoreCase))
        {
            throw new DefaultSchemaMismatch(database, schema, actual);
        }
    }

    async Task EnsureTableMigrated<TDbContext>(TDbContext context, SqlTarget target)
        where TDbContext : DbContext, ITableDbContext
    {
        if (target.ConnectionString.GetDatabaseType() != DatabaseType.Sqlite)
        {
            await context.EnsureTableExists();
            return;
        }

        var migrationLock = DatabaseMigrationLock.For(target.ConnectionString);
        await migrationLock.Semaphore.WaitAsync();
        try
        {
            RefreshMigrationCaches(migrationLock);
            await context.EnsureTableExists();
        }
        finally
        {
            migrationLock.Semaphore.Release();
        }
    }

    async Task<DbContextScope<TDbContext>> GetOrCreateTableDbContext<TDbContext>(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        ConcurrentDictionary<string, CachedDbContextOptions<TDbContext>> optionsCache,
        Func<DbContextOptions<TDbContext>, string, TDbContext> createDbContext)
        where TDbContext : DbContext, ITableDbContext
    {
        // DbContext ownership transfers to DbContextScope, which disposes it.
#pragma warning disable CA2000
        var dbContext = await CreateContext(
            new(DatabaseKind.Namespace, eventStore.Value, @namespace.Value),
            $"{eventStore.Value}:{@namespace.Value}:{tableName}",
            optionsCache,
            options => createDbContext(options, tableName),
            async (context, target) =>
            {
                // Every event store / namespace pair lives in its own database, and PostgreSQL / SQL Server
                // refuse a connection to one that does not exist yet. Namespace() creates it on the way past,
                // but nothing guarantees it ran first: appending to a namespace reaches its event-sequence
                // table through here, so a namespace whose Namespaces table was never touched arrives at a
                // database that is not there. Provision it the same way GetOrCreateReadModelDbContext does.
                await EnsureDatabaseExists(context, target);

                // Per-table migration is handled by the table-specific migrator (EnsureTableExists)
                // which already caches "already migrated" inside the migrator implementation, so we
                // only need to call it once per request — the migrator itself will fast-path on
                // subsequent calls without a DB roundtrip.
                await EnsureTableMigrated(context, target);
            });
#pragma warning restore CA2000

        return new DbContextScope<TDbContext>(dbContext, static () => { });
    }

    async Task<DbContextScope<ReadModelDbContext>> GetOrCreateReadModelDbContext(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        Func<DbContextOptions<ReadModelDbContext>, string, ReadModelDbContext> createDbContext,
        ConnectionPurpose purpose)
    {
#pragma warning disable CA2000
        var dbContext = await CreateContext(
            new(DatabaseKind.ReadModels, eventStore.Value, @namespace.Value),
            $"readmodel:{eventStore.Value}:{@namespace.Value}:{tableName}",
            _readModelOptions,
            options => createDbContext(options, tableName),
            async (context, target) =>
            {
                // Read models live in their own database (one per event store / non-default namespace
                // pair), matching the MongoDB layout. PostgreSQL / SQL Server require the database to
                // exist before any connection succeeds; SQLite creates the file on first connection.
                await EnsureDatabaseExists(context, target);
                await EnsureTableMigrated(context, target);
            },
            purpose);
#pragma warning restore CA2000

        return new DbContextScope<ReadModelDbContext>(dbContext, static () => { });
    }
}
