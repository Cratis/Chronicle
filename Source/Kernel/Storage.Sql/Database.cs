// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#if DEVELOPMENT
using System.Collections.Frozen;
using Cratis.Chronicle.Diagnostics;
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
/// wired up by <c language="csharp">WithSql</c> (e.g. <see cref="IEventSequenceMigrator"/>), so allowing the
/// convention scanner to register it causes any <c language="csharp">GetService&lt;IDatabase&gt;</c> call in non-SQL
/// modes to fail on construction with "Unable to resolve ITableMigrator&lt;&gt;" — even when the
/// caller used <c language="csharp">?.</c> expecting <see langword="null"/> for a missing registration.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/>.</param>
/// <param name="eventSequenceMigrator">The <see cref="IEventSequenceMigrator"/> for managing event sequence table migrations.</param>
/// <param name="uniqueConstraintMigrator">The <see cref="IUniqueConstraintMigrator"/> for managing unique constraint table migrations.</param>
/// <param name="readModelMigrator">The <see cref="IReadModelMigrator"/> for managing read model table migrations.</param>
[IgnoreConvention]
public partial class Database(IServiceProvider serviceProvider, IOptions<ChronicleOptions> options, IEventSequenceMigrator eventSequenceMigrator, IUniqueConstraintMigrator uniqueConstraintMigrator, IReadModelMigrator readModelMigrator) : IDatabase
{
#if DEVELOPMENT
    /// <summary>
    /// The migration that creates the single-writer lease table, kept in the SQLite cluster file's history across a wipe.
    /// </summary>
    const string SingleWriterLeaseMigrationId = "Cluster-SingleWriterLease-v19_105_0";

    /// <summary>
    /// Tables that survive <see cref="Wipe"/> for shared-database backends (PostgreSQL / Microsoft
    /// SQL Server). Identity and Data Protection key material lives in these tables; wiping them
    /// would invalidate the client's JWT (signing keys are rotated when the table is empty) and
    /// force every test-class boundary to fail with 401 even though the bootstrap handler could
    /// recreate the rows. Only compiled when the <c language="csharp">DEVELOPMENT</c> preprocessor symbol is set.
    /// </summary>
    static readonly FrozenSet<string> _preservedTables = new[]
    {
        WellKnownTableNames.Users,
        WellKnownTableNames.Applications,
        WellKnownTableNames.DataProtectionKeys,
        WellKnownTableNames.Patches,
        WellKnownTableNames.SystemInformation,
        WellKnownTableNames.EncryptionKeys,
        WellKnownTableNames.EncryptionKeyErasures,
        WellKnownTableNames.Tokens,
        WellKnownTableNames.Authorizations,
        WellKnownTableNames.Scopes,

        // The single-writer lease is cluster state held by the running kernel; deleting it makes the kernel fence itself.
        WellKnownTableNames.SingleWriterLeases,
        "__EFMigrationsHistory",
        "DatabaseNames",
        "GenerationFloor",
        "GenerationFloorHistory",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
#endif

    readonly SqlConnectionResolution _resolution = SqlConnectionResolution.From(serviceProvider, options.Value.Storage);
    readonly DatabaseNameResolver _databaseNames = new(
        SqlConnectionStrings.WithPooling(
            SqlConnectionStrings.WithSchema(options.Value.Storage.ConnectionDetails, options.Value.Storage.Schema),
            options.Value.Storage.PostgreSqlPooling),
        SqlConnectionResolution.From(serviceProvider, options.Value.Storage).PrepareRawConnection);

    readonly System.Collections.Concurrent.ConcurrentDictionary<DatabaseMigrationLock, long> _migrationGenerations = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<ClusterDbContext>> _clusterOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<EventStoreDbContext>> _eventStoreOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<NamespaceDbContext>> _namespaceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext>> _jobsOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<UniqueConstraintDbContext>> _uniqueConstraintOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<UniqueConstraintValuesDbContext>> _uniqueConstraintValuesOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<EventSequenceDbContext>> _eventSequenceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedDbContextOptions<ReadModelDbContext>> _readModelOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<(DatabaseMigrationLock Target, string Key), long> _migratedKeys = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<(DatabaseMigrationLock Target, Type Context, string Table), long> _ensuredTableGenerations = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _ensuredDatabaseKeys = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _ensuredSchemaKeys = new();

    /// <inheritdoc/>
    public TimeSpan LiveQueryPollingInterval { get; } = TimeSpan.FromSeconds(options.Value.Sql.LiveQueryPollIntervalSeconds);

    /// <summary>
    /// Gets whether runtime schema operations are read-only.
    /// </summary>
    internal bool VerifySchema => options.Value.Storage.SchemaMode == SchemaMode.Verify;

    bool CanCreateSchema => options.Value.Storage.CreateIfMissing && !VerifySchema;

    bool UsesBindings => options.Value.Storage.ConnectionDetails.GetDatabaseType() is DatabaseType.PostgreSql or DatabaseType.SqlServer;

    /// <inheritdoc/>
    public async Task<DbContextScope<ClusterDbContext>> Cluster()
    {
        if (UsesBindings)
        {
            _databaseNames.ValidateClusterName();
        }

        // DbContext ownership transfers to DbContextScope, which disposes it.
#pragma warning disable CA2000
        var dbContext = await CreateContext(
            DatabaseIdentity.Cluster,
            "cluster",
            _clusterOptions,
            static options => new ClusterDbContext(options),
            (context, target) => EnsureMigratedOnce($"cluster:{target.ConnectionString}", target, context));
#pragma warning restore CA2000
        try
        {
            if (UsesBindings && !VerifySchema)
            {
                await _databaseNames.ReserveCluster(dbContext);
            }
        }
        catch
        {
            await dbContext.DisposeAsync();
            throw;
        }

        return new DbContextScope<ClusterDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<EventStoreDbContext>> EventStore(EventStoreName eventStore)
    {
        var key = $"event-store:{eventStore.Value}";
#pragma warning disable CA2000
        var dbContext = await CreateContext(
            DatabaseIdentity.Store(eventStore.Value),
            key,
            _eventStoreOptions,
            static options => new EventStoreDbContext(options),
            (context, target) => EnsureMigratedOnce($"{key}:{target.ConnectionString}", target, context));
#pragma warning restore CA2000

        return new DbContextScope<EventStoreDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<NamespaceDbContext>> Namespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var key = $"namespace:{eventStore.Value}:{@namespace.Value}";
#pragma warning disable CA2000
        var dbContext = await CreateContext(
            new(DatabaseKind.Namespace, eventStore.Value, @namespace.Value),
            key,
            _namespaceOptions,
            static options => new NamespaceDbContext(options),
            (context, target) => EnsureMigratedOnce($"{key}:{target.ConnectionString}", target, context));
#pragma warning restore CA2000

        return new DbContextScope<NamespaceDbContext>(dbContext, static () => { });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The jobs storage migrates its own tables through these options, so they resolve for
    /// <see cref="ConnectionPurpose.Runtime"/> only.
    /// </remarks>
    public DbContextOptions<Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext> GetJobsDbContextOptions(string eventStore, string @namespace)
    {
        var identity = new DatabaseIdentity(DatabaseKind.Namespace, eventStore, @namespace);

        // Called synchronously by the jobs storage. The resolution is cached, so this only waits when it is not.
        var resolved = _resolution.Resolve(identity, ConnectionPurpose.Runtime).GetAwaiter().GetResult();
        string connectionString;
        if (SqlConnectionStrings.OwnsPlacement(resolved))
        {
            connectionString = SqlConnectionStrings.WithPlacement(options.Value.Storage.ConnectionDetails, resolved);
        }
        else if (UsesBindings)
        {
            connectionString = VerifySchema
                ? KnownConnectionString(identity)
                : _databaseNames.Known(identity, () => new ClusterDbContext(BuildOptions<ClusterDbContext>(ClusterTarget(ConnectionPurpose.Runtime))));
        }
        else
        {
            connectionString = ConnectionStringFor($"+es+{eventStore}+{@namespace}");
        }

        var schema = _resolution.SchemaFor(resolved);
        var target = new SqlTarget(
            SqlConnectionStrings.WithPooling(SqlConnectionStrings.WithSchema(connectionString, schema), options.Value.Storage.PostgreSqlPooling),
            schema,
            _resolution.IsConfigured ? _resolution.RequestFor(identity, ConnectionPurpose.Runtime) : null);
        return OptionsFor(_jobsOptions, $"jobs:{eventStore}:{@namespace}", target);
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
    public Task<DbContextScope<UniqueConstraintValuesDbContext>> UniqueConstraintValuesTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            constraintName,
            _uniqueConstraintValuesOptions,
            (options, name) => new UniqueConstraintValuesDbContext(options, name, uniqueConstraintMigrator));

    /// <inheritdoc/>
    public Task<DbContextScope<EventSequenceDbContext>> EventSequenceTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string eventSequenceName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            eventSequenceName,
            _eventSequenceOptions,
            (options, name) => new EventSequenceDbContext(options, name, eventSequenceMigrator));

    /// <inheritdoc/>
    public Task<DbContextScope<ReadModelDbContext>> ReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns, ConnectionPurpose purpose = ConnectionPurpose.Runtime) =>
        GetOrCreateReadModelDbContext(
            eventStore,
            @namespace,
            containerName,
            (options, name) => new ReadModelDbContext(options, name, columns, serviceProvider.GetRequiredService<IReadModelMigrator>()),
            purpose);

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
        _ensuredTableGenerations.Clear();

        // And clear the "database exists" cache so the next request after a Wipe re-checks
        // (the SQLite path deletes files; the PG/MsSql path truncates tables but leaves the
        // databases standing — either way we must not skip the check based on a pre-wipe
        // observation).
        _ensuredDatabaseKeys.Clear();
        _ensuredSchemaKeys.Clear();
        _databaseNames.Clear();
        _jobsOptions.Clear();
    }

#if DEVELOPMENT
    /// <inheritdoc/>
    public async Task Wipe()
    {
        var timings = new IntegrationTimings(GetType().FullName!, Environment.GetEnvironmentVariable("CHRONICLE_RUNTIME_MODE") ?? "unavailable", options.Value.Storage.Type);
        try
        {
            await timings.Run("sql-wipe-total", WipeCore);
        }
        finally
        {
            timings.Complete();
        }
    }

#endif

    /// <summary>
    /// Binds a namespace before it is published to consumers, without changing IDatabase.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The binding operation.</returns>
    internal async Task BindNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var target = await Target(new(DatabaseKind.Namespace, eventStore.Value, @namespace.Value), ConnectionPurpose.Schema);
        await using var context = new NamespaceDbContext(BuildOptions<NamespaceDbContext>(target));
        await EnsureDatabaseExists(context, target);
    }

#if DEVELOPMENT
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

    static async Task DropAllSqliteTables(string filePath, bool isClusterFile = false)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        using var timing = IntegrationTimings.BeginCurrent("sql-wipe-target-sqlite");
        timing?.Target();
        timing?.Failure();
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

        // Schema and history must disappear together, even if a DROP fails halfway through.
        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync();
        var tables = new List<string>();
        await using (var listCmd = conn.CreateCommand())
        {
            listCmd.Transaction = transaction;
            listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name = '__EFMigrationsHistory'";
            await using var reader = await listCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        var keepFloor = false;
        if (tables.Contains("GenerationFloor", StringComparer.Ordinal))
        {
            await using var floor = conn.CreateCommand();
            floor.Transaction = transaction;
            floor.CommandText = "SELECT COUNT(*) FROM GenerationFloor";
            keepFloor = Convert.ToInt64(await floor.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        // The single-writer lease is cluster state held by the running kernel; deleting it makes the kernel fence itself.
        // Keep the table and its migration-history row so the next migration does not try to create it again.
        var keepLease = isClusterFile && tables.Contains(WellKnownTableNames.SingleWriterLeases, StringComparer.Ordinal);
        if (keepLease && !keepFloor && tables.Contains("__EFMigrationsHistory", StringComparer.Ordinal))
        {
            await using var history = conn.CreateCommand();
            history.Transaction = transaction;
            history.CommandText = "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" <> $lease";
            history.Parameters.AddWithValue("$lease", SingleWriterLeaseMigrationId);
            await history.ExecuteNonQueryAsync();
        }

        timing?.TablesObserved();
        foreach (var table in tables.Where(_ =>
            (!keepFloor || (_ != "GenerationFloor" && _ != "GenerationFloorHistory" && _ != "__EFMigrationsHistory"))
            && (!keepLease || (_ != WellKnownTableNames.SingleWriterLeases && _ != "__EFMigrationsHistory"))))
        {
            await using var dropCmd = conn.CreateCommand();
            dropCmd.Transaction = transaction;
#pragma warning disable CA2100 // Table names come from sqlite_master in this same file — there is no user input.
            dropCmd.CommandText = $"{(keepFloor ? "DELETE FROM" : "DROP TABLE IF EXISTS")} \"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
#pragma warning restore CA2100
            await dropCmd.ExecuteNonQueryAsync();
            timing?.Table();
        }

        await transaction.CommitAsync();
        timing?.Success();
    }

    static async Task WipeAllPostgreSqlDatabases(string clusterConnectionString, IEnumerable<string> boundDatabases)
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

        var databasesToWipe = new HashSet<string>(boundDatabases, StringComparer.Ordinal) { clusterDb };
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
        using var timing = IntegrationTimings.BeginCurrent("sql-wipe-target-postgresql");
        timing?.Target();
        timing?.Failure();
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
        timing?.Success();
    }

    static async Task WipeAllMsSqlDatabases(string clusterConnectionString, IEnumerable<string> boundDatabases)
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

        var databasesToWipe = new HashSet<string>(boundDatabases, StringComparer.Ordinal) { clusterDb };
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
        using var timing = IntegrationTimings.BeginCurrent("sql-wipe-target-mssql");
        timing?.Target();
        timing?.Failure();
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
        timing?.Success();
    }
#endif

    static bool IsAlreadyExistsException(Exception? ex) => SchemaObjectAlreadyExists.Is(ex);

#if DEVELOPMENT
    async Task WipeCore()
    {
        if (options.Value.Clustering.Type == ClusteringType.Sql)
        {
            throw new SqlClusteringPreventsKernelStateReset();
        }

        foreach (var target in (await GenerationStoreTargets()).Values)
        {
            await using var context = new EventStoreDbContext(ReadOptions<EventStoreDbContext>(target));
            try
            {
                if (await context.GenerationInitializationReceipts.AnyAsync())
                {
                    throw new InvalidOperationException("Kernel state reset is refused for an initialized generation store.");
                }
            }
            catch (Exception error) when (MissingGenerationTable(error))
            {
                // The legacy store has never been initialized.
            }
        }
        var storageType = options.Value.Storage.Type;
        var connectionDetails = options.Value.Storage.ConnectionDetails;
        var isSqlite = string.Equals(storageType, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase);
        var boundDatabases = UsesBindings ? await BoundDatabases() : [];

        if (isSqlite)
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
            await WipeAllPostgreSqlDatabases(connectionDetails, boundDatabases);

            // Every database and namespace schema has a connection string of its own, so each one keeps an
            // Npgsql pool of its own. A wipe keeps the databases, and without releasing the idle connections
            // here every reset carries the previous run's pools forward until the server refuses new clients.
            NpgsqlConnection.ClearAllPools();
        }
        else if (string.Equals(storageType, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            await WipeAllMsSqlDatabases(connectionDetails, boundDatabases);
        }

        // SQLite invalidates its caches under each target's migration lock during the wipe.
        if (!isSqlite)
        {
            ClearTableMigrationCache(string.Empty);
        }

        serviceProvider.GetService<Cratis.Orleans.Storage.IJobsStorage>()?.Reset();
    }

    async Task<string[]> BoundDatabases()
    {
        await using var scope = await Cluster();
        return await scope.DbContext.DatabaseNames.Select(row => row.PhysicalName).ToArrayAsync();
    }
#endif

    /// <summary>
    /// Builds the connection string for a per-event-store or per-namespace database.
    /// Every event store and namespace lives in its own database — mirroring MongoDB's
    /// <c language="csharp">{eventStore}+es</c> and <c language="csharp">{eventStore}+es+{namespace}</c> database layout — so that
    /// tables like <c language="csharp">Namespaces</c>, <c language="csharp">Reactors</c>, and <c language="csharp">EventStoreSubscriptions</c> cannot
    /// collide across event stores on the same SQL server. The suffix is appended to the cluster
    /// database name from configuration so the kernel can co-exist with other Chronicle instances
    /// on the same server (e.g., the in-process test silo and the out-of-process kernel both
    /// using one PostgreSQL container).
    /// </summary>
    /// <param name="suffix">Suffix to append to the cluster database name (e.g. <c language="csharp">+es+Testing</c>).</param>
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
