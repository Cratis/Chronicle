// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the SQL sink on PostgreSQL, where constraint names are
/// unique across a schema rather than per table as they are on SQLite.
/// </summary>
public class PostgreSqlSinkHarness : ISinkHarness
{
    /// <summary>
    /// One container for the whole run; every harness gets its own database in it.
    /// </summary>
    static readonly Lazy<Task<PostgreSqlContainer>> _container = new(StartContainer);

    IReadOnlyList<ProjectedColumn> _columns = [];
    string _connectionString = string.Empty;

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        _connectionString = CreateDatabase().GetAwaiter().GetResult();

        var database = Substitute.For<IDatabase>();
        database.LiveQueryPollingInterval.Returns(TimeSpan.FromMilliseconds(50));
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(callInfo => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(callInfo.ArgAt<string>(2)), () => { })));

        return new SqlSink(
            "test-event-store",
            "test-namespace",
            definition,
            database,
            new ExpandoObjectConverter(new TypeFormats()));
    }

    /// <inheritdoc/>
    public void Dispose() => GC.SuppressFinalize(this);

    static async Task<PostgreSqlContainer> StartContainer()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();
        return container;
    }

    static async Task<string> CreateDatabase()
    {
        var container = await _container.Value;
        var databaseName = $"sink_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The database name is a generated identifier.
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = databaseName }.ToString();
    }

    bool TableExists(ReadModelDbContext context, string tableName) =>
        context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_tables WHERE schemaname = current_schema() AND tablename = {tableName}")
            .AsEnumerable()
            .Single() > 0;

    ReadModelDbContext CreateContext(string containerName)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseNpgsql(_connectionString)
            .AddConceptAsSupport()
            .Options;

        var context = new ReadModelDbContext(options, containerName, _columns, Substitute.For<IReadModelMigrator>());

        // As in the SQLite harness: the real database creates a container's table through the migrator on first
        // use, which the substitute does not, and ending a replay renames tables out from under any memo.
        if (!TableExists(context, containerName))
        {
            context.Database.ExecuteSqlRaw(context.Database.GenerateCreateScript());
        }

        return context;
    }
}
