// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the SQL sink, backed by an in-memory SQLite database.
/// </summary>
public class SqlSinkHarness : ISinkHarness
{
    readonly SqliteConnection _connection = new("DataSource=:memory:");
    IReadOnlyList<ProjectedColumn> _columns = [];

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        _connection.Open();

        // Honors the container the sink asks for rather than always handing back the main one: a replay
        // writes to its own container and the sink swaps that in when the replay ends, so a harness that
        // ignored the name would fail every replay case for a reason that has nothing to do with the sink.
        var database = Substitute.For<IDatabase>();
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
    public void Dispose() => _connection.Dispose();

    bool TableExists(string tableName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "select count(*) from sqlite_master where type = 'table' and name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    ReadModelDbContext CreateContext(string containerName)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        var context = new ReadModelDbContext(options, containerName, _columns, Substitute.For<IReadModelMigrator>());

        // The real database creates a container's table on first use through the migrator; the substitute
        // does not, so the harness creates it here. Existence is asked of the database rather than
        // remembered, because ending a replay renames tables out from under any memo of what exists.
        if (!TableExists(containerName))
        {
            context.Database.ExecuteSqlRaw(context.Database.GenerateCreateScript());
        }

        return context;
    }
}
