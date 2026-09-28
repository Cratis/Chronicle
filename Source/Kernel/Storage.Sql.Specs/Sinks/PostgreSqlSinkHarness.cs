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
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the SQL sink on PostgreSQL, where constraint names are
/// unique across a schema rather than per table as they are on SQLite.
/// </summary>
/// <remarks>
/// The fixture is a property rather than a constructor argument because the contract creates the harness
/// itself; a case needing the container overrides that and hands one over. Every harness gets a database of
/// its own in the fixture's container, dropped again when the harness is disposed.
/// </remarks>
public class PostgreSqlSinkHarness : ISinkHarness
{
    IReadOnlyList<ProjectedColumn> _columns = [];

    /// <summary>
    /// Gets or sets the <see cref="PostgreSqlFixture"/> supplying the container.
    /// </summary>
    public PostgreSqlFixture? Fixture { get; set; }

    /// <summary>
    /// Gets the connection string for the database the sink writes to.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        ConnectionString = Fixture!.CreateDatabase().GetAwaiter().GetResult();

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

    /// <summary>
    /// Creates a context for a container in the sink's database, creating its table when it is not there.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A <see cref="ReadModelDbContext"/> for the container.</returns>
    public ReadModelDbContext CreateContext(string containerName)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseNpgsql(ConnectionString)
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Fixture is not null && ConnectionString.Length > 0)
        {
            Fixture.DropDatabase(ConnectionString).GetAwaiter().GetResult();
        }

        GC.SuppressFinalize(this);
    }

    static bool TableExists(ReadModelDbContext context, string tableName)
    {
        var storedName = PrimaryKeyNames.TableIdentifier(Arc.EntityFrameworkCore.DatabaseType.PostgreSql, tableName);
        return context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_tables WHERE schemaname = current_schema() AND tablename = {storedName}")
            .AsEnumerable()
            .Single() > 0;
    }
}
