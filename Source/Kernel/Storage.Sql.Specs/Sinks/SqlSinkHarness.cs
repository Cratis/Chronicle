// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    string _containerName = string.Empty;
    IReadOnlyList<ProjectedColumn> _columns = [];

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _containerName = definition.ContainerName;
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));

        return new SqlSink(
            "test-event-store",
            "test-namespace",
            definition,
            database,
            new ExpandoObjectConverter(new TypeFormats()));
    }

    /// <inheritdoc/>
    public void Dispose() => _connection.Dispose();

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new ReadModelDbContext(options, _containerName, _columns, Substitute.For<IReadModelMigrator>());
    }
}
