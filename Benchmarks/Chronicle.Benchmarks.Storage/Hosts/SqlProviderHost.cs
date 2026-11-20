// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Hosts SQLite (file), PostgreSQL or SQL Server and builds real Chronicle SQL storage on it, with an EF Core
/// interceptor counting every command.
/// </summary>
/// <param name="provider">Which SQL provider to host.</param>
internal sealed class SqlProviderHost(StorageProvider provider) : IStorageProviderHost
{
    const string PostgreSqlImage = "postgres:16-alpine";
    readonly List<EventSequenceStorageHost> _hosts = [];
    readonly HashSet<string> _ensuredTables = [];
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"chronicle-storage-bench-{Guid.NewGuid():N}");
    readonly UniqueConstraintMigrator _migrator = new(
        new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance),
        new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance),
        NullLogger<UniqueConstraintMigrator>.Instance);
    IContainer? _container;
    string _connectionString = string.Empty;
    IInterceptor[] _interceptors = [];

    /// <inheritdoc/>
    public StorageProvider Provider => provider;

    /// <inheritdoc/>
    public CommandCounter Counter { get; } = new();

    /// <inheritdoc/>
    public async Task Start()
    {
        _interceptors = CountingInterceptors.For(Counter);
        var password = $"Benchmark-{Guid.NewGuid():N}!";
        switch (provider)
        {
            case StorageProvider.Sqlite:
                Directory.CreateDirectory(_directory);
                _connectionString = $"Data Source={Path.Combine(_directory, "events.db")}";
                break;

            case StorageProvider.PostgreSql:
                var postgres = new PostgreSqlBuilder(PostgreSqlImage).WithDatabase("chronicle").WithPassword(password).WithLogger(NullLogger.Instance).Build();
                _container = postgres;
                await postgres.StartAsync();
                _connectionString = postgres.GetConnectionString();
                break;

            case StorageProvider.SqlServer:
                var sql = new MsSqlBuilder(Integration.SqlServerContainerImage.Name)
                    .WithPassword(password)
                    .WithLogger(NullLogger.Instance)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("SQL Server is now ready for client connections", wait => wait.WithTimeout(TimeSpan.FromSeconds(90))))
                    .Build();
                _container = sql;
                await sql.StartAsync();
                _connectionString = sql.GetConnectionString();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }
    }

    /// <inheritdoc/>
    public IEventSequenceStorage CreateSequence()
    {
        var host = new EventSequenceStorageHost(Configure<Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.EventSequenceDbContext>, null, BenchmarkEvents.CreateIdentities(), BenchmarkEvents.CreateEventTypes(), _interceptors);
        Counter.Reset();
        _hosts.Add(host);
        return host.Open();
    }

    /// <inheritdoc/>
    public async Task<(IUniqueConstraintsStorage Storage, UniqueConstraintDefinition Definition)> CreateConstraints()
    {
        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(call => CreateConstraintScope(call.ArgAt<string>(2)));
        var name = $"unique-{Guid.NewGuid():N}";
        var definition = new UniqueConstraintDefinition(name, [new UniqueConstraintEventDefinition(BenchmarkEvents.Selected.Id, ["value"])]);
        var storage = new UniqueConstraintsStorage("bench-store", "bench-namespace", EventSequenceId.Log, database);
        await Task.CompletedTask;
        return (storage, definition);
    }

    /// <inheritdoc/>
    public async Task<SettingsLedger> BuildLedger(StorageRunOptions options, LoadReading loadAtStart)
    {
        var durability = new Dictionary<string, string>();
        string version;
        string durabilityClass;
        string image;
        string pool;
        var digest = HostFacts.Unknown;
        switch (provider)
        {
            case StorageProvider.Sqlite:
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var synchronous = Convert.ToInt32(await Scalar(connection, "PRAGMA synchronous"), System.Globalization.CultureInfo.InvariantCulture);
                    var journal = (await Scalar(connection, "PRAGMA journal_mode"))?.ToString() ?? HostFacts.Unknown;
                    version = (await Scalar(connection, "select sqlite_version()"))?.ToString() ?? HostFacts.Unknown;
                    durability["synchronous"] = synchronous.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    durability["journal_mode"] = journal;
                    durabilityClass = DurabilityClassifier.ForSqlite(synchronous, journal);
                }

                image = "file-backed (Microsoft.Data.Sqlite)";
                pool = $"Pooling={new SqliteConnectionStringBuilder(_connectionString).Pooling} (Microsoft.Data.Sqlite; pool size is not configurable)";
                break;

            case StorageProvider.PostgreSql:
                await using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var commit = (await Scalar(connection, "SHOW synchronous_commit"))?.ToString() ?? HostFacts.Unknown;
                    var fsync = (await Scalar(connection, "SHOW fsync"))?.ToString() ?? HostFacts.Unknown;
                    version = (await Scalar(connection, "SHOW server_version"))?.ToString() ?? HostFacts.Unknown;
                    durability["synchronous_commit"] = commit;
                    durability["fsync"] = fsync;
                    durabilityClass = DurabilityClassifier.ForPostgreSql(commit, fsync);
                }

                image = PostgreSqlImage;
                pool = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"MaxPoolSize={new NpgsqlConnectionStringBuilder(_connectionString).MaxPoolSize} (connection string in effect)");
                digest = HostFacts.ImageDigest(image);
                break;

            default:
                await using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var delayed = (await Scalar(connection, "select delayed_durability_desc from sys.databases where name = DB_NAME()"))?.ToString() ?? HostFacts.Unknown;
                    version = (await Scalar(connection, "select cast(serverproperty('ProductVersion') as nvarchar(64))"))?.ToString() ?? HostFacts.Unknown;
                    durability["delayed_durability"] = delayed;
                    durabilityClass = DurabilityClassifier.ForSqlServer(delayed);
                }

                image = Integration.SqlServerContainerImage.Name;
                pool = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"MaxPoolSize={new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString).MaxPoolSize} (connection string in effect)");
                digest = HostFacts.ImageDigest(image);
                break;
        }

        return HostFacts.Ledger(provider, options, loadAtStart, new(new(image, digest, version), new(durability, durabilityClass), pool));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var host in _hosts) host.Dispose();
        SqliteConnection.ClearAllPools();
        if (_container is not null) await _container.DisposeAsync();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    /// <summary>Gets the runtime type of the connection the kernel's contexts get, which must be the provider's own.</summary>
    /// <returns>The connection type.</returns>
    public Type ConnectionType()
    {
        var builder = new DbContextOptionsBuilder<DbContext>();
        Configure(builder);
        builder.AddInterceptors(_interceptors);
        using var context = new DbContext(builder.Options);
        return context.Database.GetDbConnection().GetType();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Only called with constant diagnostic queries.")]
    static async Task<object?> Scalar(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }

    void Configure<TContext>(DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        _ = provider switch
        {
            StorageProvider.Sqlite => builder.UseSqlite(_connectionString),
            StorageProvider.PostgreSql => builder.UseNpgsql(_connectionString),
            _ => builder.UseSqlServer(_connectionString)
        };
        builder.EnableServiceProviderCaching(true);
    }

    async Task<DbContextScope<UniqueConstraintDbContext>> CreateConstraintScope(string tableName)
    {
        var builder = new DbContextOptionsBuilder<UniqueConstraintDbContext>();
        Configure(builder);
        builder.AddConceptAsSupport().AddInterceptors(_interceptors);
#pragma warning disable CA2000 // The scope returned below owns the context.
        var context = new UniqueConstraintDbContext(builder.Options, tableName, _migrator);
#pragma warning restore CA2000
        bool ensure;
        lock (_ensuredTables) ensure = _ensuredTables.Add(tableName);
        if (ensure) await context.EnsureTableExists();
        return new DbContextScope<UniqueConstraintDbContext>(context, () => { });
    }
}
