// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the SQL sink on PostgreSQL, where constraint names are
/// unique across a schema rather than per table as they are on SQLite.
/// </summary>
/// <remarks>
/// The fixture is a property rather than a constructor argument because the contract creates the harness
/// itself; a case needing the container overrides that and hands one over. Every harness gets a database of
/// its own in the fixture's container, dropped again when the harness is disposed. Tables are created through
/// the real <see cref="ReadModelMigrator"/>, as in production. Container names must keep their replay, backup
/// and shadow table names within PostgreSQL's 63 bytes (48 bytes or less); longer ones fail in the migrator
/// (issue #4340).
/// </remarks>
public class PostgreSqlSinkHarness : ISinkHarness
{
    readonly ReadModelMigrator _migrator = new(
        new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
        Substitute.For<ILogger<ReadModelMigrator>>());

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
            .Returns(callInfo => OpenTable(callInfo.ArgAt<string>(2)));

        return new SqlSink(
            "test-event-store",
            "test-namespace",
            definition,
            database,
            new ExpandoObjectConverter(new TypeFormats()),
            new ReplayingTables());
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

    /// <summary>
    /// Opens a container in the sink's database, migrating its table as the real database does on every use.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A <see cref="DbContextScope{TDbContext}"/> for the container.</returns>
    async Task<DbContextScope<ReadModelDbContext>> OpenTable(string containerName)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseNpgsql(ConnectionString)
            .AddConceptAsSupport()
            .Options;

#pragma warning disable CA2000 // Disposed by the sink through the returned scope.
        var context = new ReadModelDbContext(options, containerName, _columns, _migrator);
#pragma warning restore CA2000
        await context.EnsureTableExists();
        return new DbContextScope<ReadModelDbContext>(context, () => { });
    }
}
