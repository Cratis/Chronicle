// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Places fixture tables in one database while sharing the production resolver and its persisted ledger.
/// </summary>
/// <param name="configure">Configures contexts for the fixture's database.</param>
/// <param name="connectionString">The physical connection string.</param>
/// <param name="migrator">The real read model migrator.</param>
internal sealed class SinkReadModelTablePlacements(Action<DbContextOptionsBuilder> configure, string connectionString, IReadModelMigrator migrator) : IReadModelTablePlacements
{
    readonly ITableMigrator<ReadModelTableBindingsDbContext> _ledgerMigrator =
        new TableMigrator<ReadModelTableBindingsDbContext>(NullLogger<TableMigrator<ReadModelTableBindingsDbContext>>.Instance);

    ReadModelTables? _tables;
    Exception? _nextOpenFailure;
    DatabaseType? _provider;

    internal int LedgerOpens { get; private set; }

    internal int TableOpens { get; private set; }

    internal int Targets { get; private set; }

    internal IReadModelTables Tables => LazyInitializer.EnsureInitialized(ref _tables, () => new ReadModelTables(this));

    string ConnectionString => connectionString;

    DatabaseType Provider
    {
        get
        {
            if (_provider is not null)
            {
                return _provider.Value;
            }
            using var context = new EventStoreDbContext(Options<EventStoreDbContext>());
            _provider = context.Database.GetDatabaseType();
            return _provider.Value;
        }
    }

    IReadModelMigrator Migrator => migrator;

    /// <inheritdoc/>
    public ValueTask<ReadModelTablePlace> Locate(string eventStore, string namespaceName, ConnectionPurpose purpose)
    {
        Targets++;
        return new ValueTask<ReadModelTablePlace>(new Place(this, eventStore, namespaceName));
    }

    internal void FailNextOpen(Exception error) => _nextOpenFailure = error;

    internal bool HasPendingOpenFailure => _nextOpenFailure is not null;

    /// <summary>
    /// Uses an existing fixture connection with the real resolver and migrator.
    /// </summary>
    /// <param name="connection">The fixture connection.</param>
    /// <param name="definition">The registered read model.</param>
    /// <param name="configure">The fixture's context configuration.</param>
    /// <returns>The production accessor at the fixture's placement.</returns>
    internal static async Task<IReadModelTables> ForExisting(DbConnection connection, ReadModelDefinition definition, Action<DbContextOptionsBuilder> configure) =>
        (await PlacementsForExisting(connection, definition, configure)).Tables;

    /// <summary>
    /// Creates the fixture placements and registers the definition that proves ownership of a pre-existing live table.
    /// </summary>
    /// <param name="connection">The fixture connection.</param>
    /// <param name="definition">The definition owning the existing table.</param>
    /// <param name="configure">The fixture's context configuration.</param>
    /// <returns>The placements, so a spec can inject failures.</returns>
    internal static async Task<SinkReadModelTablePlacements> PlacementsForExisting(DbConnection connection, ReadModelDefinition definition, Action<DbContextOptionsBuilder> configure)
    {
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        var placements = new SinkReadModelTablePlacements(configure, connection.ConnectionString, migrator);
        await placements.Register(definition);
        return placements;
    }

    /// <summary>
    /// Persists the definition that proves ownership of any pre-existing live table.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Register(ReadModelDefinition definition)
    {
        await using var context = new EventStoreDbContext(Options<EventStoreDbContext>());
        var sqlServer = context.Database.GetDatabaseType() == DatabaseType.SqlServer;
        var helper = context.GetService<Microsoft.EntityFrameworkCore.Storage.ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier("ReadModelDefinitions");
        var identifier = helper.DelimitIdentifier("Id");
        var name = helper.DelimitIdentifier("Name");
        var sinkType = helper.DelimitIdentifier("SinkType");
        var nameType = sqlServer ? "nvarchar(max)" : "text";
        var create = $"CREATE TABLE {table} ({identifier} varchar(255) NOT NULL PRIMARY KEY, {name} {nameType} NOT NULL, {sinkType} varchar(255) NOT NULL)";
        await context.Database.ExecuteSqlRawAsync(sqlServer
            ? $"IF OBJECT_ID(N'ReadModelDefinitions', N'U') IS NULL {create}"
            : create.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS", StringComparison.Ordinal));
        var id = definition.Identifier.Value;
        var container = definition.ContainerName.Value;
        var delete = $"DELETE FROM {table} WHERE {identifier} = {{0}}";
        var insert = $"INSERT INTO {table} ({identifier}, {name}, {sinkType}) VALUES ({{0}}, {{1}}, {{2}})";
        await context.Database.ExecuteSqlRawAsync(delete, id);
        await context.Database.ExecuteSqlRawAsync(insert, id, container, "SQL");
    }

    DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        configure(builder);
        builder.WithoutServiceProviderCaching();
        return builder.Options;
    }

    sealed class Place(SinkReadModelTablePlacements owner, string eventStore, string namespaceName) : ReadModelTablePlace
    {
        internal override ReadModelTablePlacement? Placement => owner.Provider == DatabaseType.Sqlite
            ? new ReadModelTablePlacement(DatabaseType.Sqlite, new SqliteConnectionStringBuilder(owner.ConnectionString).DataSource, string.Empty, string.Empty, Schema)
            : ReadModelTablePlacement.Of(owner.ConnectionString, Schema);

        internal override string Schema => owner.Provider switch
        {
            DatabaseType.Sqlite => "main",
            DatabaseType.SqlServer => "dbo",
            _ => "public"
        };

        internal override ITableMigrator<ReadModelTableBindingsDbContext> LedgerMigrator => owner._ledgerMigrator;

        internal override async Task<ReadModelDbContext> OpenTable(string physicalName, IReadOnlyList<ProjectedColumn> columns, bool requireExisting)
        {
            owner.TableOpens++;
            if (owner._nextOpenFailure is { } error)
            {
                owner._nextOpenFailure = null;
                throw error;
            }

            var context = new ReadModelDbContext(owner.Options<ReadModelDbContext>(), physicalName, columns, owner.Migrator) { RequireExisting = requireExisting };
            try
            {
                await context.EnsureTableExists();
                return context;
            }
            catch
            {
                await context.DisposeAsync();
                throw;
            }
        }

        internal override Task<ReadModelTableBindingsDbContext> OpenLedger()
        {
            owner.LedgerOpens++;
            return Task.FromResult(new ReadModelTableBindingsDbContext(owner.Options<ReadModelTableBindingsDbContext>(), Schema));
        }

        internal override Task<IReadOnlyList<ReadModelTableMetadataScope>> Scopes() =>
            Task.FromResult<IReadOnlyList<ReadModelTableMetadataScope>>([
                new(eventStore, namespaceName, new EventStoreDbContext(owner.Options<EventStoreDbContext>()), new NamespaceDbContext(owner.Options<NamespaceDbContext>()))
            ]);
    }
}
