// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.given;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

#pragma warning disable CA2100 // Fixture-owned identifiers only.

/// <summary>
/// Both claim table shapes of one event sequence on SQL Server, with every command that reaches them counted.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
public abstract class a_server_claims_storage(SqlServerFixture fixture) : Specification
{
    protected static readonly string _sourceTable = $"{EventSequenceId.Log}_unique_constraint";
    protected static readonly string _valuesTable = $"{EventSequenceId.Log}_versions_constraint_values";
    protected CommandCounter _counter = new();
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _perSource = new("unique", []);
    protected UniqueConstraintDefinition _perValue = new("versions", []) { Mode = UniqueConstraintMode.PerValue };
    protected string _connectionString;
    protected UniqueConstraintMigrator _migrator;
    DbContextOptions<UniqueConstraintDbContext> _sourceOptions;
    DbContextOptions<UniqueConstraintValuesDbContext> _valuesOptions;
    DbContextOptions<NamespaceDbContext> _namespaceOptions;

    protected async Task Prepare(bool createTables, ILogger<UniqueConstraintsStorage>? logger = null, string store = "store")
    {
        _connectionString = await fixture.CreateDatabase();
        _migrator = new(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());
        _sourceOptions = new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlServer(_connectionString).AddInterceptors(_counter).WithoutServiceProviderCaching().Options;
        _valuesOptions = new DbContextOptionsBuilder<UniqueConstraintValuesDbContext>().UseSqlServer(_connectionString).AddInterceptors(_counter).WithoutServiceProviderCaching().Options;
        _namespaceOptions = new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;

        if (createTables)
        {
            await EnsureTables();
        }

        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(new DbContextScope<UniqueConstraintDbContext>(new(_sourceOptions, call.ArgAt<string>(2), _migrator), () => { })));
        database.UniqueConstraintValuesTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(new DbContextScope<UniqueConstraintValuesDbContext>(new(_valuesOptions, call.ArgAt<string>(2), _migrator), () => { })));
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new(_namespaceOptions), () => { })));
        _storage = new(store, "namespace", EventSequenceId.Log, database, logger);
    }

    protected async Task EnsureTables()
    {
        await using var source = new UniqueConstraintDbContext(_sourceOptions, _sourceTable, _migrator);
        await source.EnsureTableExists();
        await using var values = new UniqueConstraintValuesDbContext(_valuesOptions, _valuesTable, _migrator);
        await values.EnsureTableExists();
    }

    /// <summary>
    /// Creates a per-event-source table the way it looked before owners were kept exactly.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <returns>Awaitable task.</returns>
    protected Task CreateLegacySourceTable(string table) => Execute($"CREATE TABLE [{table}] ([EventSourceId] NVARCHAR(200) NOT NULL PRIMARY KEY, [Value] NVARCHAR(MAX) NOT NULL, [SequenceNumber] DECIMAL(20,0) NOT NULL)");

    /// <summary>
    /// Creates a per-value table the way it looked before owners were kept exactly.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <returns>Awaitable task.</returns>
    protected Task CreateLegacyValuesTable(string table) => Execute($"CREATE TABLE [{table}] ([Value] NVARCHAR(200) NOT NULL PRIMARY KEY, [EventSourceId] NVARCHAR(200) NOT NULL, [SequenceNumber] DECIMAL(20,0) NOT NULL)");

    protected Task AddLegacyClaim(string table, string owner, string value) =>
        Execute($"INSERT INTO [{table}] ([EventSourceId], [Value], [SequenceNumber]) VALUES (N'{owner}', N'{value}', 42)");

    protected async Task Execute(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    protected async Task<long> Scalar(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    protected Task<long> Rows(string table) => Scalar($"SELECT COUNT(*) FROM [{table}]");

    protected Task<long> Pending(string table) => Scalar($"SELECT COUNT(*) FROM [{table}] WHERE [ExactSpelling] = 0");

    protected Task<long> Exact(string table) => Scalar($"SELECT COUNT(*) FROM [{table}] WHERE [ExactSpelling] = 1");

    protected async Task<int> Commands(Func<Task> action)
    {
        var before = _counter.Count;
        await action();
        return _counter.Count - before;
    }

    Task Destroy() => string.IsNullOrEmpty(_connectionString) ? Task.CompletedTask : fixture.QueueDatabaseDrop(_connectionString);
}
