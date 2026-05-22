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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<ClusterDbContext>> _clusterOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<EventStoreDbContext>> _eventStoreOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<NamespaceDbContext>> _namespaceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<UniqueConstraintDbContext>> _uniqueConstraintOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<EventSequenceDbContext>> _eventSequenceOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DbContextOptions<ReadModelDbContext>> _readModelOptions = new();
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _migratedKeys = new();

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
    public Task<DbContextScope<ReadModelDbContext>> ReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            containerName,
            _readModelOptions,
            (options, name) => new ReadModelDbContext(options, name, serviceProvider.GetRequiredService<IReadModelMigrator>()));

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
    }

    /// <summary>
    /// Serializes EF Core <c>MigrateAsync</c> calls per connection string to prevent concurrent migration race conditions
    /// when multiple grains access the same database simultaneously.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to migrate.</param>
    /// <param name="connectionString">The connection string used as the lock key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    static async Task MigrateWithLock(DbContext context, string connectionString)
    {
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

    string GetConnectionStringForEventStore(EventStoreName eventStore)
    {
        var databaseType = options.Value.Storage.ConnectionDetails.GetDatabaseType();
        if (databaseType == DatabaseType.Sqlite)
        {
            return ReplaceFilename(eventStore);
        }

        return options.Value.Storage.ConnectionDetails;
    }

    string GetConnectionStringForEventStoreAndNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var databaseType = options.Value.Storage.ConnectionDetails.GetDatabaseType();
        if (databaseType == DatabaseType.Sqlite)
        {
            return ReplaceFilename($"{eventStore}_{@namespace}");
        }

        return options.Value.Storage.ConnectionDetails;
    }

    string ReplaceFilename(string postfix)
    {
        if (TryReplaceFilename("Data Source", postfix, out var dataSource))
        {
            return dataSource;
        }

        if (TryReplaceFilename("Filename", postfix, out var filename))
        {
            return filename;
        }

        return options.Value.Storage.ConnectionDetails;
    }

    bool TryReplaceFilename(string keyToReplace, string postfix, [NotNullWhen(true)] out string? connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = options.Value.Storage.ConnectionDetails
        };

        if (builder.TryGetValue(keyToReplace, out var dataSource))
        {
            var originalFilename = dataSource.ToString();
            var directory = Path.GetDirectoryName(originalFilename) ?? string.Empty;
            var newFilename = $"{Path.GetFileNameWithoutExtension(originalFilename)}_{postfix}{Path.GetExtension(originalFilename)}";
            builder[keyToReplace] = Path.Combine(directory, newFilename);
            connectionString = builder.ConnectionString;
            return true;
        }

        connectionString = null;
        return false;
    }
}
