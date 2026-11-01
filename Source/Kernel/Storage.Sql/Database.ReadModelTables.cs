// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that places read model tables and resolves their physical names.
/// </summary>
public partial class Database : IReadModelTablePlacements
{
    readonly ConcurrentDictionary<(string EventStore, string Namespace, string Container), Task<ReadModelTableHandle>> _liveHandles = new();
    readonly ConcurrentDictionary<(string EventStore, string Namespace, ConnectionPurpose Purpose), DatabaseTablePlace> _tablePlaces = new();
    ReadModelTables? _tables;

    /// <summary>
    /// Gets the application services the database resolves its migrators from.
    /// </summary>
    internal IServiceProvider Services => serviceProvider;

    /// <summary>
    /// Gets the read model tables that resolve logical identities to physical names.
    /// </summary>
    internal IReadModelTables Tables => LazyInitializer.EnsureInitialized(ref _tables, () => new ReadModelTables(this));

    /// <inheritdoc/>
    async ValueTask<ReadModelTablePlace> IReadModelTablePlacements.Locate(string eventStore, string namespaceName, ConnectionPurpose purpose)
    {
        // The target is always resolved afresh, as it is for every other context, so a re-resolved connection is never missed.
        // The place built from it is reused while the target is unchanged, so a warm lookup allocates nothing of its own.
        var key = (eventStore, namespaceName, purpose);
        if (_tablePlaces.TryGetValue(key, out var cached))
        {
            var current = await Target(cached.Identity, purpose);
            if (cached.IsAt(current))
            {
                return cached;
            }

            return _tablePlaces[key] = new DatabaseTablePlace(this, cached.Identity, current, purpose);
        }

        var identity = new DatabaseIdentity(DatabaseKind.ReadModels, eventStore, namespaceName);
        return _tablePlaces[key] = new DatabaseTablePlace(this, identity, await Target(identity, purpose), purpose);
    }

    async Task<DbContextScope<ReadModelDbContext>> OpenLiveReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns, ConnectionPurpose purpose)
    {
        var key = (eventStore.Value, @namespace.Value, containerName);
        ReadModelTableHandle handle;
        try
        {
            handle = await _liveHandles.GetOrAdd(key, static (scope, database) => database.FindLiveHandle(scope.EventStore, scope.Namespace, scope.Container), this);
        }
        catch
        {
            _liveHandles.TryRemove(key, out _);
            throw;
        }

        return await Tables.Open(handle, columns, purpose);
    }

    async Task<ReadModelTableHandle> FindLiveHandle(string eventStore, string namespaceName, string containerName)
    {
        await using var scope = await EventStore(eventStore);
        var name = new Concepts.ReadModels.ReadModelContainerName(containerName);
        var definitions = await scope.DbContext.ReadModels.AsNoTracking()
            .Where(definition => definition.SinkType == WellKnownSinkTypes.SQL.Value && definition.Name == name)
            .Select(definition => definition.Id)
            .ToArrayAsync();
        if (definitions.Length != 1)
        {
            var owner = new ReadModelTableOwner(eventStore, namespaceName, string.Empty, containerName, ReadModelTableRole.Live, containerName);
            throw new ReadModelTableNameBindingFailure(owner, containerName, definitions.Length == 0 ? "no SQL read model has this container" : "several SQL read models share this container");
        }

        return ReadModelTableHandle.For(eventStore, namespaceName, definitions[0].Value, containerName, ReadModelTableRole.Live, containerName);
    }

    async Task<ReadModelDbContext> OpenReadModelContext(SqlTarget target, DatabaseIdentity identity, string physicalName, IReadOnlyList<ProjectedColumn> columns, bool requireExisting, ConnectionPurpose purpose)
    {
#pragma warning disable CA2000
        return await CreateContext(
            target,
            identity,
            $"readmodel:{identity.EventStore}:{identity.Namespace}:{physicalName}",
            _readModelOptions,
            options => new ReadModelDbContext(options, physicalName, columns, serviceProvider.GetRequiredService<IReadModelMigrator>()) { RequireExisting = requireExisting },
            async (context, prepared) =>
            {
                // Read models live in their own database (one per event store / non-default namespace
                // pair), matching the MongoDB layout. PostgreSQL / SQL Server require the database to
                // exist before any connection succeeds; SQLite creates the file on first connection.
                await EnsureDatabaseExists(context, prepared);
                await EnsureTableMigrated(context, prepared);
            },
            purpose);
#pragma warning restore CA2000
    }

    sealed class DatabaseTablePlace(Database database, DatabaseIdentity identity, SqlTarget target, ConnectionPurpose purpose) : ReadModelTablePlace
    {
        internal DatabaseIdentity Identity => identity;

        internal override ReadModelTablePlacement? Placement { get; } = ReadModelTablePlacement.Of(target.ConnectionString, target.Schema);

        internal override string Schema => target.Schema;

        internal override ITableMigrator<ReadModelTableBindingsDbContext> LedgerMigrator =>
            database.Services.GetRequiredService<ITableMigrator<ReadModelTableBindingsDbContext>>();

        internal bool IsAt(SqlTarget other) => string.Equals(other.ConnectionString, target.ConnectionString, StringComparison.Ordinal) && string.Equals(other.Schema, target.Schema, StringComparison.Ordinal);

        internal override Task<ReadModelDbContext> OpenTable(string physicalName, IReadOnlyList<ProjectedColumn> columns, bool requireExisting) =>
            database.OpenReadModelContext(target, identity, physicalName, columns, requireExisting, purpose);

        internal override async Task<ReadModelTableBindingsDbContext> OpenLedger()
        {
            var schemaTarget = purpose == ConnectionPurpose.Schema || !database._resolution.IsConfigured
                ? target
                : await database.Target(identity, ConnectionPurpose.Schema);
            var ledger = new ReadModelTableBindingsDbContext(database.BuildOptions<ReadModelTableBindingsDbContext>(schemaTarget), schemaTarget.Schema);
            try
            {
                await database.EnsureDatabaseExists(ledger, schemaTarget);
                return ledger;
            }
            catch
            {
                await ledger.DisposeAsync();
                throw;
            }
        }

        internal override Task<IReadOnlyList<ReadModelTableMetadataScope>> Scopes() =>
            database.ReadModelTableMetadataScopes(identity, Placement);
    }
}
