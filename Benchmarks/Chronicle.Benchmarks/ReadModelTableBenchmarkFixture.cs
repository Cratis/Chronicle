// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;
using ReadModelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// A SQLite-backed SQL storage with seeded stores and namespaces, used by the read model table benchmarks.
/// </summary>
/// <remarks>
/// Uses public API only, so the identical file compiles on the baseline and on the patch.
/// </remarks>
public sealed class ReadModelTableBenchmarkFixture : IDisposable
{
    const string SchemaJson = """{ "type": "object", "properties": { "count": { "type": "integer" } } }""";
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"chronicle-bench-{Guid.NewGuid():N}");
    readonly string _connectionString;
    readonly ReadModelDefinition _definition;
    readonly Key _existingKey = new("counter-1", ArrayIndexers.NoIndexers);
    readonly Key _missingKey = new("missing", ArrayIndexers.NoIndexers);
    readonly ReplayingTables _replayingTables = new();
    readonly SharedReadModelFileResolver _resolver;
    ServiceProvider? _services;
    int _count;

    ReadModelTableBenchmarkFixture(string container)
    {
        Directory.CreateDirectory(_directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "cluster.db"), Pooling = false }.ConnectionString;
        _resolver = new(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "read-models.db"), Pooling = false }.ConnectionString);
        _definition = DefinitionFor("bench-read-model", container);
    }

    /// <summary>
    /// Gets the database of the current instance.
    /// </summary>
    public Database Database { get; private set; } = null!;

    /// <summary>
    /// Gets the sink of the current instance.
    /// </summary>
    public Sink Sink { get; private set; } = null!;

    /// <summary>
    /// Gets the name of the store the sink and the scope benchmarks use.
    /// </summary>
    public static string StoreName => StoreFor(0);

    /// <summary>
    /// Gets the name of the namespace the sink and the scope benchmarks use.
    /// </summary>
    public static string NamespaceName => NamespaceFor(0);

    /// <summary>
    /// Gets the container the read model is stored in.
    /// </summary>
    public string Container => _definition.ContainerName.Value;

    /// <summary>
    /// Creates a seeded fixture.
    /// </summary>
    /// <param name="container">The read model container name.</param>
    /// <param name="stores">The number of event stores to seed.</param>
    /// <param name="namespaces">The number of namespaces per store to seed.</param>
    /// <returns>The fixture with a first instance opened.</returns>
    public static async Task<ReadModelTableBenchmarkFixture> Create(string container, int stores, int namespaces)
    {
        var fixture = new ReadModelTableBenchmarkFixture(container);
        fixture.NewInstance();
        await fixture.Seed(stores, namespaces);
        fixture.NewInstance();
        return fixture;
    }

    /// <summary>
    /// Disposes the current instance and builds a fresh database and sink over the same files, as after a restart.
    /// </summary>
    public void NewInstance()
    {
        _services?.Dispose();
        SqliteConnection.ClearAllPools();
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_resolver)
            .AddSingleton(typeof(ITableMigrator<>), typeof(TableMigrator<>))
            .AddSingleton<IReadModelMigrator>(_ => new ReadModelMigrator(
                new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
                NullLogger<ReadModelMigrator>.Instance))
            .BuildServiceProvider();
        Database = new(
            _services,
            Options.Create(new Cratis.Chronicle.Configuration.ChronicleOptions { Storage = new() { Type = Cratis.Chronicle.Storage.StorageType.Sqlite, ConnectionDetails = _connectionString, ConnectionResolution = Resolution() } }),
            new NoEventSequenceMigrator(),
            new NoUniqueConstraintMigrator(),
            _services.GetRequiredService<IReadModelMigrator>());
        Sink = new(StoreName, NamespaceName, _definition, Database, new ExpandoObjectConverter(new TypeFormats()), _replayingTables);
    }

    /// <summary>
    /// Opens and disposes a table scope through the database string overload.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task OpenScope()
    {
        await using var scope = await Database.ReadModelTable(StoreName, NamespaceName, Container, ProjectedColumns.ForSchema(_definition.GetSchemaForLatestGeneration(), _definition.Indexes));
    }

    /// <summary>
    /// Finds a missing key through the sink.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Find() => Sink.FindOrDefault(_missingKey);

    /// <summary>
    /// Applies a one-property change to the existing key through the sink.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Apply()
    {
        var current = _count;
        _count++;
        return Sink.ApplyChanges(_existingKey, Changeset(current, _count), EventSequenceNumber.First);
    }

    /// <summary>
    /// Deletes every row of the table-name ledger, where the database has one, so the next first open is unbound.
    /// </summary>
    public void DeleteLedger()
    {
        foreach (var file in Directory.GetFiles(_directory, "*.db"))
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ConnectionString);
            connection.Open();
            using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'chronicle_read_model_table_names'";
            if (Convert.ToInt64(exists.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0)
            {
                using var delete = connection.CreateCommand();
                delete.CommandText = "DELETE FROM chronicle_read_model_table_names";
                delete.ExecuteNonQuery();
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _services?.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    static ReadModelDefinition DefinitionFor(string identifier, string container) => new(
        identifier,
        container,
        "Benchmark read model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL),
        new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromJson(SchemaJson) } },
        []);

    static ConnectionResolutionOptions Resolution()
    {
        var resolution = new ConnectionResolutionOptions();
        resolution.Connection.UseCustom<SharedReadModelFileResolver>();
        return resolution;
    }

    static string StoreFor(int index) => $"bench-store-{index}";

    static string NamespaceFor(int index) => index == 0 ? "Default" : $"ns-{index}";

    static Changeset<Concepts.Events.AppendedEvent, ExpandoObject> Changeset(int from, int to)
    {
        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)["count"] = from;
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = to;
        var changeset = new Changeset<Concepts.Events.AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, initial);
        changeset.Add(new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("count"), from, to)]));
        return changeset;
    }

    async Task Seed(int stores, int namespaces)
    {
        await using (var cluster = await Database.Cluster())
        {
            for (var store = 0; store < stores; store++)
            {
                cluster.DbContext.EventStores.Add(new() { Name = StoreFor(store) });
            }
            await cluster.DbContext.SaveChangesAsync();
        }

        for (var store = 0; store < stores; store++)
        {
            await using (var scope = await Database.EventStore(StoreFor(store)))
            {
                for (var @namespace = 0; @namespace < namespaces; @namespace++)
                {
                    scope.DbContext.Namespaces.Add(new() { Name = NamespaceFor(@namespace), Created = DateTimeOffset.UtcNow });
                }
                await scope.DbContext.SaveChangesAsync();
            }

            // Every other store registers its own container in the shared file, so each one is a candidate owner.
            await new ReadModelDefinitionsStorage(StoreFor(store), Database).Save(store == 0 ? _definition : DefinitionFor($"bench-read-model-{store}", $"other_{store}"));
        }

        // Create the table with a row to update, so the measured apply path is an update.
        await Sink.ApplyChanges(_existingKey, Changeset(0, 0), EventSequenceNumber.First);
    }

    sealed class SharedReadModelFileResolver(string connectionString) : IConnectionResolver
    {
        public Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.Scope[Cratis.Chronicle.Configuration.StorageConnectionRequests.Kind] == "readModels"
                ? new ResolvedConnection(ConnectionString: new SqliteConnectionStringBuilder(connectionString)
                {
                    DataSource = new SqliteConnectionStringBuilder(connectionString).DataSource + "." + request.Scope[StorageConnectionRequests.Namespace] + ".db"
                }.ConnectionString)
                : ResolvedConnection.Unchanged);
    }

    sealed class NoEventSequenceMigrator : IEventSequenceMigrator
    {
        public Task EnsureTableMigrated(string tableName, EventSequenceDbContext context) => Task.CompletedTask;

        public void ClearMigrationCache(string connectionStringPrefix)
        {
        }
    }

    sealed class NoUniqueConstraintMigrator : IUniqueConstraintMigrator
    {
        public Task EnsureValuesTableMigrated(string tableName, UniqueConstraintValuesDbContext context) => Task.CompletedTask;

        public Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context) => Task.CompletedTask;

        public void ClearMigrationCache(string connectionStringPrefix)
        {
        }
    }
}
