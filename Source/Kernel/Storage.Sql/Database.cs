// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#if DEVELOPMENT
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
#endif

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IDatabase"/>.
/// </summary>
/// <remarks>
/// Marked with <see cref="IgnoreConventionAttribute"/> so convention binding does not register it
/// automatically. The SQL database has hard constructor dependencies on services that are only
/// wired up by <c>WithSql</c> (e.g. <see cref="IEventSequenceMigrator"/>), so allowing the
/// convention scanner to register it causes any <c>GetService&lt;IDatabase&gt;</c> call in non-SQL
/// modes to fail on construction with "Unable to resolve ITableMigrator&lt;&gt;" — even when the
/// caller used <c>?.</c> expecting <see langword="null"/> for a missing registration.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/>.</param>
/// <param name="eventSequenceMigrator">The <see cref="IEventSequenceMigrator"/> for managing event sequence table migrations.</param>
/// <param name="uniqueConstraintMigrator">The <see cref="IUniqueConstraintMigrator"/> for managing unique constraint table migrations.</param>
/// <param name="readModelMigrator">The <see cref="IReadModelMigrator"/> for managing read model table migrations.</param>
[IgnoreConvention]
public class Database(IServiceProvider serviceProvider, IOptions<ChronicleOptions> options, IEventSequenceMigrator eventSequenceMigrator, IUniqueConstraintMigrator uniqueConstraintMigrator, IReadModelMigrator readModelMigrator) : IDatabase
{
    static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _migrationLocks = new();

#if DEVELOPMENT
    /// <summary>
    /// Tables that survive <see cref="Wipe"/> for shared-database backends (PostgreSQL / Microsoft
    /// SQL Server). Identity and Data Protection key material lives in these tables; wiping them
    /// would invalidate the client's JWT (signing keys are rotated when the table is empty) and
    /// force every test-class boundary to fail with 401 even though the bootstrap handler could
    /// recreate the rows. Only compiled when the <c>DEVELOPMENT</c> preprocessor symbol is set.
    /// </summary>
    static readonly HashSet<string> _preservedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        WellKnownTableNames.Users,
        WellKnownTableNames.Applications,
        WellKnownTableNames.DataProtectionKeys,
        WellKnownTableNames.Patches,
        WellKnownTableNames.SystemInformation,
        WellKnownTableNames.EncryptionKeys,
        WellKnownTableNames.Tokens,
        WellKnownTableNames.Authorizations,
        WellKnownTableNames.Scopes,
        "__EFMigrationsHistory",
    };
#endif

    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<ClusterDbContext>> _clusterOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<EventStoreDbContext>> _eventStoreOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<NamespaceDbContext>> _namespaceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<UniqueConstraintDbContext>> _uniqueConstraintOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<EventSequenceDbContext>> _eventSequenceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<ReadModelDbContext>> _readModelOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _migratedKeys = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _ensuredDatabaseKeys = new();

    /// <inheritdoc/>
    public async Task<DbContextScope<ClusterDbContext>> Cluster()
    {
        var connectionString = options.Value.Storage.ConnectionDetails;
        var key = $"cluster:{connectionString}";
        var dbContextOptions = _clusterOptions.GetOrAdd(
            key,
            static (_, args) => BuildOptions<ClusterDbContext>(args.serviceProvider, args.connectionString),
            (serviceProvider, connectionString));

        // DbContext ownership transfers to DbContextScope, which disposes it.
#pragma warning disable CA2000
        var dbContext = new ClusterDbContext(dbContextOptions);
#pragma warning restore CA2000
        await EnsureMigratedOnce(key, connectionString, dbContext);
        return new DbContextScope<ClusterDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<EventStoreDbContext>> EventStore(EventStoreName eventStore)
    {
        var connectionString = GetConnectionStringForEventStore(eventStore);
        var key = $"event-store:{eventStore.Value}:{connectionString}";
        var dbContextOptions = _eventStoreOptions.GetOrAdd(
            key,
            static (_, args) => BuildOptions<EventStoreDbContext>(args.serviceProvider, args.connectionString),
            (serviceProvider, connectionString));

#pragma warning disable CA2000
        var dbContext = new EventStoreDbContext(dbContextOptions);
#pragma warning restore CA2000
        await EnsureMigratedOnce(key, connectionString, dbContext);
        return new DbContextScope<EventStoreDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<NamespaceDbContext>> Namespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var connectionString = GetConnectionStringForEventStoreAndNamespace(eventStore, @namespace);
        var key = $"namespace:{eventStore.Value}:{@namespace.Value}:{connectionString}";
        var dbContextOptions = _namespaceOptions.GetOrAdd(
            key,
            static (_, args) => BuildOptions<NamespaceDbContext>(args.serviceProvider, args.connectionString),
            (serviceProvider, connectionString));

#pragma warning disable CA2000
        var dbContext = new NamespaceDbContext(dbContextOptions);
#pragma warning restore CA2000
        await EnsureMigratedOnce(key, connectionString, dbContext);
        return new DbContextScope<NamespaceDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    public Task<DbContextScope<UniqueConstraintDbContext>> UniqueConstraintTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            constraintName,
            _uniqueConstraintOptions,
            (options, name) => new UniqueConstraintDbContext(options, name, uniqueConstraintMigrator));

    /// <inheritdoc/>
    public Task<DbContextScope<EventSequenceDbContext>> EventSequenceTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string eventSequenceName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            eventSequenceName,
            _eventSequenceOptions,
            (options, name) => new EventSequenceDbContext(options, name, eventSequenceMigrator));

    /// <inheritdoc/>
    public Task<DbContextScope<ReadModelDbContext>> ReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns) =>
        GetOrCreateReadModelDbContext(
            eventStore,
            @namespace,
            containerName,
            (options, name) => new ReadModelDbContext(options, name, columns, serviceProvider.GetRequiredService<IReadModelMigrator>()));

    /// <inheritdoc/>
    public void ClearTableMigrationCache(string connectionStringPrefix)
    {
        eventSequenceMigrator.ClearMigrationCache(connectionStringPrefix);
        uniqueConstraintMigrator.ClearMigrationCache(connectionStringPrefix);
        readModelMigrator.ClearMigrationCache(connectionStringPrefix);

        // Also clear the per-context migration cache so the next request re-runs EF Core
        // migrations. SQLite tests delete the database file between test classes; without
        // this the in-process silo would skip migration and then query non-existent tables.
        _migratedKeys.Clear();

        // And clear the "database exists" cache so the next request after a Wipe re-checks
        // (the SQLite path deletes files; the PG/MsSql path truncates tables but leaves the
        // databases standing — either way we must not skip the check based on a pre-wipe
        // observation).
        _ensuredDatabaseKeys.Clear();
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task Wipe()
    {
        var storageType = options.Value.Storage.Type;
        var connectionDetails = options.Value.Storage.ConnectionDetails;

        if (string.Equals(storageType, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            // SQLite: each event store, namespace, and read-model namespace lives in its own
            // file. The cluster file path is the configured Data Source; the derived files
            // sit alongside it with a "+" suffix in the basename. We drop every table inside
            // each file via a fresh single-writer connection rather than File.Delete because
            // grain activations in the same process may still hold open handles to the
            // database when the wipe runs. File.Delete unlinks the path on Linux/macOS but
            // leaves the inode live for the open handle — and the next SQLite open against
            // the path can intermittently fail with "disk I/O error" or "attempt to write a
            // readonly database" while the file system juggles the recreate. DROP TABLE
            // serializes through SQLite's own RESERVED/EXCLUSIVE locks, so it is safe under
            // concurrent grain access, leaves the file intact, and never triggers WAL
            // recovery against a vanished main file.
            SqliteConnection.ClearAllPools();
            await WipeAllSqliteDatabases(connectionDetails);
        }
        else if (string.Equals(storageType, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            await WipeAllPostgreSqlDatabases(connectionDetails);
        }
        else if (string.Equals(storageType, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            await WipeAllMsSqlDatabases(connectionDetails);
        }

        // Cache invalidation must happen AFTER the wipe so that any in-flight migration that
        // populates the cache concurrently with the wipe is overwritten by a final empty cache.
        ClearTableMigrationCache(string.Empty);
    }

    static string ExtractSqliteDataSource(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (builder.TryGetValue("Data Source", out var value) || builder.TryGetValue("Filename", out value))
        {
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    static string PreservedNamesForSql() =>
        string.Join(", ", _preservedTables.Select(t => $"'{t}'"));

    static async Task WipeAllSqliteDatabases(string clusterConnectionString)
    {
        var clusterPath = ExtractSqliteDataSource(clusterConnectionString);
        if (string.IsNullOrEmpty(clusterPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(clusterPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        var baseName = Path.GetFileNameWithoutExtension(clusterPath);
        var extension = Path.GetExtension(clusterPath);

        // Enumerate every SQLite database file in the cluster directory whose name starts
        // with the cluster basename. Sidecar files (-wal, -shm, -journal) are filtered out
        // — they aren't database files themselves and SQLite manages them as part of the
        // main file's lifecycle.
        var pattern = string.IsNullOrEmpty(extension) ? $"{baseName}*" : $"{baseName}*{extension}";
        foreach (var file in Directory.GetFiles(directory, pattern))
        {
            if (file.EndsWith("-wal", StringComparison.Ordinal)
                || file.EndsWith("-shm", StringComparison.Ordinal)
                || file.EndsWith("-journal", StringComparison.Ordinal))
            {
                continue;
            }

            await DropAllSqliteTables(file);
        }
    }

    static async Task DropAllSqliteTables(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        await using var conn = new SqliteConnection($"Data Source={filePath};Pooling=False");
        try
        {
            await conn.OpenAsync();
        }
        catch (SqliteException)
        {
            // File is unreadable as a SQLite database — skip it; the next migration will
            // re-create whatever schema it needs.
            return;
        }

        var tables = new List<string>();
        await using (var listCmd = conn.CreateCommand())
        {
            listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in tables)
        {
            await using var dropCmd = conn.CreateCommand();
#pragma warning disable CA2100 // Table names come from sqlite_master in this same file — there is no user input.
            dropCmd.CommandText = $"DROP TABLE IF EXISTS \"{table}\"";
#pragma warning restore CA2100
            await dropCmd.ExecuteNonQueryAsync();
        }
    }

    static async Task WipeAllPostgreSqlDatabases(string clusterConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(clusterConnectionString);
        var clusterDb = builder.Database;
        if (string.IsNullOrEmpty(clusterDb))
        {
            return;
        }

        // Switch to the maintenance database so we can enumerate sibling databases that share
        // the cluster prefix (the per-event-store and per-namespace databases created at runtime).
        builder.Database = "postgres";
        await using var maintenanceConn = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await maintenanceConn.OpenAsync();
        }
        catch (NpgsqlException)
        {
            // Server unreachable on the first wipe — nothing to do.
            return;
        }

        var databasesToWipe = new List<string> { clusterDb };
#pragma warning disable CA2100
        await using (var listCmd = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datname LIKE @prefix || '+%' AND datname <> @cluster", maintenanceConn))
#pragma warning restore CA2100
        {
            listCmd.Parameters.AddWithValue("prefix", clusterDb);
            listCmd.Parameters.AddWithValue("cluster", clusterDb);
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databasesToWipe.Add(reader.GetString(0));
            }
        }

        await maintenanceConn.CloseAsync();

        foreach (var database in databasesToWipe)
        {
            builder.Database = database;
            await TruncateAllPostgreSqlTables(builder.ConnectionString);
        }
    }

    static async Task TruncateAllPostgreSqlTables(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (NpgsqlException)
        {
            // Database may not exist yet on the first wipe — nothing to truncate.
            return;
        }

        var preserved = PreservedNamesForSql();
        var sql =
            "DO $$ DECLARE r RECORD; " +
            "BEGIN " +
            "    FOR r IN (SELECT tablename FROM pg_tables " +
            $"              WHERE schemaname = 'public' AND tablename NOT IN ({preserved})) LOOP " +
            "        EXECUTE 'TRUNCATE TABLE \"' || r.tablename || '\" CASCADE'; " +
            "    END LOOP; " +
            "END $$;";
#pragma warning disable CA2100 // Preserved-table names are compile-time constants from WellKnownTableNames.
        await using var cmd = new NpgsqlCommand(sql, conn);
#pragma warning restore CA2100
        await cmd.ExecuteNonQueryAsync();
    }

    static async Task WipeAllMsSqlDatabases(string clusterConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(clusterConnectionString);
        var clusterDb = builder.InitialCatalog;
        if (string.IsNullOrEmpty(clusterDb))
        {
            return;
        }

        builder.InitialCatalog = "master";
        await using var maintenanceConn = new SqlConnection(builder.ConnectionString);
        try
        {
            await maintenanceConn.OpenAsync();
        }
        catch (SqlException)
        {
            return;
        }

        var databasesToWipe = new List<string> { clusterDb };
#pragma warning disable CA2100
        await using (var listCmd = new SqlCommand("SELECT name FROM sys.databases WHERE name LIKE @prefix + '+%' AND name <> @cluster", maintenanceConn))
#pragma warning restore CA2100
        {
            listCmd.Parameters.AddWithValue("@prefix", clusterDb);
            listCmd.Parameters.AddWithValue("@cluster", clusterDb);
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databasesToWipe.Add(reader.GetString(0));
            }
        }

        await maintenanceConn.CloseAsync();

        foreach (var database in databasesToWipe)
        {
            builder.InitialCatalog = database;
            await TruncateAllMsSqlTables(builder.ConnectionString);
        }
    }

    static async Task TruncateAllMsSqlTables(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (SqlException)
        {
            return;
        }

        var preserved = PreservedNamesForSql();
        var sql =
            "DECLARE @sql NVARCHAR(MAX) = N''; " +
            "SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] NOCHECK CONSTRAINT ALL;' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "SELECT @sql += N'DELETE FROM [' + s.name + N'].[' + t.name + N'];' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "EXEC sp_executesql @sql;";
#pragma warning disable CA2100 // Preserved-table names are compile-time constants from WellKnownTableNames.
        await using var cmd = new SqlCommand(sql, conn);
#pragma warning restore CA2100
        await cmd.ExecuteNonQueryAsync();
    }
#endif

    static bool IsAlreadyExistsException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }

    static DbContextOptions<TDbContext> BuildOptions<TDbContext>(IServiceProvider serviceProvider, string connectionString)
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseDatabaseFromConnectionString(connectionString);
        builder
            .UseApplicationServiceProvider(serviceProvider)
            .AddConceptAsSupport();

        // SQLite serializes writers on a database-wide lock; the kernel writes from many grains at
        // once, so without WAL + a busy timeout concurrent writes fail with SQLITE_BUSY and stall
        // observer catch-up. The interceptor applies those pragmas on every SQLite connection.
        if (connectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            builder.AddInterceptors(SqlitePragmaConnectionInterceptor.Instance);
        }

        return builder.Options;
    }

    async Task EnsureMigratedOnce(string key, string connectionString, DbContext context)
    {
        if (_migratedKeys.ContainsKey(key))
        {
            return;
        }

        await MigrateWithLock(context, connectionString);
        _migratedKeys.TryAdd(key, true);
    }

    /// <summary>
    /// Serializes EF Core <c>MigrateAsync</c> calls per connection string to prevent concurrent migration race conditions
    /// when multiple grains access the same database simultaneously.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to migrate.</param>
    /// <param name="connectionString">The connection string used as the lock key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task MigrateWithLock(DbContext context, string connectionString)
    {
        // For SQLite the file is created automatically when EF Core opens a connection
        // to a non-existent path. PostgreSQL and SQL Server require the database to
        // exist before a connection succeeds, so explicitly create it first when the
        // event-store / namespace targets a per-database isolation name that has not
        // been provisioned yet. This mirrors MongoDB, where each event store and each
        // namespace lives in its own database that the kernel creates on first access.
        await EnsureDatabaseExists(context, connectionString);

        var migrationLock = _migrationLocks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
        await migrationLock.WaitAsync();
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex) when (IsAlreadyExistsException(ex))
        {
            // A concurrent migration already completed and created the tables.
            // This is safe to ignore: the schema is in the correct final state.
        }
        finally
        {
            migrationLock.Release();
        }
    }

    async Task EnsureDatabaseExists(DbContext context, string connectionString)
    {
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
        if (_ensuredDatabaseKeys.ContainsKey(connectionString))
        {
            return;
        }

        var migrationLock = _migrationLocks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
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
                try
                {
                    await creator.CreateAsync();
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

    async Task<DbContextScope<TDbContext>> GetOrCreateTableDbContext<TDbContext>(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<TDbContext>> optionsCache,
        Func<DbContextOptions<TDbContext>, string, TDbContext> createDbContext)
        where TDbContext : DbContext, ITableDbContext
    {
        var connectionString = GetConnectionStringForEventStoreAndNamespace(eventStore, @namespace);
        var key = $"{eventStore.Value}:{@namespace.Value}:{tableName}:{connectionString}";
        var dbContextOptions = optionsCache.GetOrAdd(
            key,
            static (_, args) => BuildOptions<TDbContext>(args.serviceProvider, args.connectionString),
            (serviceProvider, connectionString));

        // DbContext ownership transfers to DbContextScope, which disposes it.
#pragma warning disable CA2000
        var dbContext = createDbContext(dbContextOptions, tableName);
#pragma warning restore CA2000

        // Per-table migration is handled by the table-specific migrator (EnsureTableExists)
        // which already caches "already migrated" inside the migrator implementation, so we
        // only need to call it once per request — the migrator itself will fast-path on
        // subsequent calls without a DB roundtrip.
        await dbContext.EnsureTableExists();
        return new DbContextScope<TDbContext>(dbContext, static () => { });
    }

    async Task<DbContextScope<ReadModelDbContext>> GetOrCreateReadModelDbContext(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        Func<DbContextOptions<ReadModelDbContext>, string, ReadModelDbContext> createDbContext)
    {
        var connectionString = GetConnectionStringForReadModels(eventStore, @namespace);
        var key = $"readmodel:{eventStore.Value}:{@namespace.Value}:{tableName}:{connectionString}";
        var dbContextOptions = _readModelOptions.GetOrAdd(
            key,
            static (_, args) => BuildOptions<ReadModelDbContext>(args.serviceProvider, args.connectionString),
            (serviceProvider, connectionString));

#pragma warning disable CA2000
        var dbContext = createDbContext(dbContextOptions, tableName);
#pragma warning restore CA2000

        // Read models live in their own database (one per event store / non-default namespace
        // pair), matching the MongoDB layout. PostgreSQL / SQL Server require the database to
        // exist before any connection succeeds; SQLite creates the file on first connection.
        await EnsureDatabaseExists(dbContext, connectionString);

        await dbContext.EnsureTableExists();
        return new DbContextScope<ReadModelDbContext>(dbContext, static () => { });
    }

    string GetConnectionStringForEventStore(EventStoreName eventStore) =>
        ConnectionStringFor($"+es+{eventStore.Value}");

    string GetConnectionStringForEventStoreAndNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        ConnectionStringFor($"+es+{eventStore.Value}+{@namespace.Value}");

    /// <summary>
    /// Builds the connection string for the read-model database of a given event store and namespace.
    /// Mirrors MongoDB's read-model database naming: <c>{eventStore}</c> for the default namespace and
    /// <c>{eventStore}+{namespace}</c> for non-default namespaces. The cluster database name is
    /// always prefixed (separated by <c>+</c>) so SQL backends — which serve every Chronicle
    /// instance from the same server — can co-exist without colliding on read-model database names.
    /// </summary>
    /// <param name="eventStore">The event store the read model belongs to.</param>
    /// <param name="namespace">The namespace the read model belongs to.</param>
    /// <returns>A connection string targeting the read-model database.</returns>
    string GetConnectionStringForReadModels(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        @namespace == EventStoreNamespaceName.Default
            ? ConnectionStringFor($"+{eventStore.Value}")
            : ConnectionStringFor($"+{eventStore.Value}+{@namespace.Value}");

    /// <summary>
    /// Builds the connection string for a per-event-store or per-namespace database.
    /// Every event store and namespace lives in its own database — mirroring MongoDB's
    /// <c>{eventStore}+es</c> and <c>{eventStore}+es+{namespace}</c> database layout — so that
    /// tables like <c>Namespaces</c>, <c>Reactors</c>, and <c>EventStoreSubscriptions</c> cannot
    /// collide across event stores on the same SQL server. The suffix is appended to the cluster
    /// database name from configuration so the kernel can co-exist with other Chronicle instances
    /// on the same server (e.g., the in-process test silo and the out-of-process kernel both
    /// using one PostgreSQL container).
    /// </summary>
    /// <param name="suffix">Suffix to append to the cluster database name (e.g. <c>+es+Testing</c>).</param>
    /// <returns>A connection string targeting the derived database.</returns>
    string ConnectionStringFor(string suffix)
    {
        var connectionString = options.Value.Storage.ConnectionDetails;
        var databaseType = connectionString.GetDatabaseType();
        return databaseType switch
        {
            DatabaseType.Sqlite => ReplaceFilename(suffix),
            DatabaseType.PostgreSql => AppendToDatabaseName("Database", suffix),
            DatabaseType.SqlServer => AppendToDatabaseName(GetSqlServerDatabaseKey(), suffix),
            _ => connectionString,
        };
    }

    string AppendToDatabaseName(string key, string suffix)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = options.Value.Storage.ConnectionDetails };
        if (!builder.TryGetValue(key, out var current))
        {
            return options.Value.Storage.ConnectionDetails;
        }
        builder[key] = $"{current}{suffix}";
        return builder.ConnectionString;
    }

    string GetSqlServerDatabaseKey()
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = options.Value.Storage.ConnectionDetails };
        return builder.ContainsKey("Initial Catalog") ? "Initial Catalog" : "Database";
    }

    string ReplaceFilename(string suffix)
    {
        if (TryReplaceFilename("Data Source", suffix, out var dataSource))
        {
            return dataSource;
        }

        if (TryReplaceFilename("Filename", suffix, out var filename))
        {
            return filename;
        }

        return options.Value.Storage.ConnectionDetails;
    }

    bool TryReplaceFilename(string keyToReplace, string suffix, [NotNullWhen(true)] out string? connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = options.Value.Storage.ConnectionDetails
        };

        if (builder.TryGetValue(keyToReplace, out var dataSource))
        {
            var originalFilename = dataSource.ToString() ?? string.Empty;
            var directory = Path.GetDirectoryName(originalFilename) ?? string.Empty;
            var newFilename = $"{Path.GetFileNameWithoutExtension(originalFilename)}{suffix}{Path.GetExtension(originalFilename)}";
            builder[keyToReplace] = Path.Combine(directory, newFilename);
            connectionString = builder.ConnectionString;
            return true;
        }

        connectionString = null;
        return false;
    }
}
