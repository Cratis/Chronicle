// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the SQL sink, backed by a file-based SQLite database in WAL mode.
/// </summary>
public class SqlSinkHarness : ISqlSinkHarness
{
    readonly ReadModelMigrator _migrator = new(
        new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
        Substitute.For<ILogger<ReadModelMigrator>>());
    readonly ReplayingTables _replayingTables = new();
    readonly string _path = Path.Combine(Path.GetTempPath(), $"chronicle-sink-{Guid.NewGuid():N}.db");
    IReadOnlyList<ProjectedColumn> _columns = [];
    SqliteConnection? _connection;
    ReadModelDefinition? _definition;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSinkHarness"/> class.
    /// </summary>
    public SqlSinkHarness() => ConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = _path,
        Cache = SqliteCacheMode.Private,
        Pooling = false
    }.ToString();

    /// <summary>
    /// Gets the isolated database shared by this harness's connections.
    /// </summary>
    public string ConnectionString { get; init; }

    /// <summary>
    /// Gets the recommender handed to the sinks the harness creates, for specs that observe replay recommendations.
    /// </summary>
    public ICompositeKeyReplayRecommender? CompositeKeyReplayRecommender { get; init; }

    /// <summary>
    /// Gets interceptors that let concurrency specs arrange an exact command interleaving.
    /// </summary>
    public IEnumerable<IInterceptor> Interceptors { get; init; } = [];

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _definition = definition;
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        if (_connection is null)
        {
            _connection = new SqliteConnection(ConnectionString);
            _connection.Open();
            using var command = _connection.CreateCommand();

            // Like production, WAL and private caches let separate connections read the committed schema
            // while replay promotion holds a schema write lock inside its transaction.
            command.CommandText = "PRAGMA journal_mode=WAL";
            command.ExecuteNonQuery();
        }

        // Honors the container the sink asks for rather than always handing back the main one: a replay
        // writes to its own container and the sink swaps that in when the replay ends, so a harness that
        // ignored the name would fail every replay case for a reason that has nothing to do with the sink.
        var database = Substitute.For<IDatabase>();
        database.LiveQueryPollingInterval.Returns(TimeSpan.FromMilliseconds(50));
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>())
            .Returns(async callInfo => new DbContextScope<ReadModelDbContext>(await CreateContext(callInfo.ArgAt<string>(2)), () => { }));

        return new SqlSink(
            "test-event-store",
            "test-namespace",
            definition,
            database,
            new ExpandoObjectConverter(new TypeFormats()),
            _replayingTables,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlSink>.Instance,
            CompositeKeyReplayRecommender);
    }

    /// <summary>
    /// Creates another sink for the read model the last sink was created for, over the same database.
    /// </summary>
    /// <returns>A separate <see cref="ISink"/> instance writing to the same tables.</returns>
    /// <remarks>
    /// The kernel does not hand every caller the same sink instance: the projection pipeline, the replay handler
    /// and the read model store each ask for one, and a pipeline built earlier keeps the sink it was built with.
    /// </remarks>
    public ISink CreateSinkForTheSameReadModel() => CreateSink(_definition!);

    /// <summary>
    /// Reads the stored columns without schema conversion.
    /// </summary>
    /// <returns>The rows in the read model's primary table.</returns>
    public async Task<DynamicReadModelEntity[]> ReadStoredRows()
    {
        await using var context = await CreateContext(_definition!.ContainerName.Value);
        return await context.Entries.AsNoTracking().ToArrayAsync();
    }

    /// <summary>
    /// Stores a row directly under an id, the way a sink of an earlier version stored a composite key.
    /// </summary>
    /// <param name="id">The id to store the row under.</param>
    /// <param name="name">The value of the name column.</param>
    /// <returns>Awaitable task.</returns>
    public async Task StoreRowUnderId(string id, string name)
    {
        await using var context = await CreateContext(_definition!.ContainerName.Value);
        var row = new DynamicReadModelEntity();
        foreach (var column in _columns.Where(column => !column.IsNullable))
        {
            row[column.Name] = column.ClrType.IsValueType ? Activator.CreateInstance(column.ClrType) : null;
        }

        row[_columns.First(column => column.IsKey).Name] = id;
        row["name"] = name;
        context.Entries.Add(row);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        _connection?.Dispose();
        File.Delete(_path);
        File.Delete($"{_path}-wal");
        File.Delete($"{_path}-shm");
    }

    async Task<ReadModelDbContext> CreateContext(string containerName)
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(ConnectionString);
        var options = builder.AddConceptAsSupport()
            .AddInterceptors(Interceptors)
            .Options;

        var context = new ReadModelDbContext(options, containerName, _columns, _migrator);
        await context.EnsureTableExists();
        return context;
    }
}
