// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs SQL sink specs with SQL Server's production execution strategy and table migrator.
/// </summary>
public class SqlServerSinkHarness : ISqlSinkHarness
{
    readonly ReadModelMigrator _migrator = new(
        new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
        Substitute.For<ILogger<ReadModelMigrator>>());
    IReadOnlyList<ProjectedColumn> _columns = [];

    /// <inheritdoc/>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the container supplying isolated databases.
    /// </summary>
    public SqlServerFixture? Fixture { get; init; }

    /// <summary>
    /// Gets interceptors for deterministic faults during replay promotion.
    /// </summary>
    public IEnumerable<IInterceptor> Interceptors { get; init; } = [];

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration());
        if (ConnectionString.Length == 0)
        {
            ConnectionString = Fixture!.CreateDatabase().GetAwaiter().GetResult();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(callInfo => OpenTable(callInfo.ArgAt<string>(2)));

        return new Sink(
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

    async Task<DbContextScope<ReadModelDbContext>> OpenTable(string containerName)
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(ConnectionString);
        var options = builder.AddConceptAsSupport().AddInterceptors(Interceptors).Options;

#pragma warning disable CA2000 // Disposed by the sink through the returned scope.
        var context = new ReadModelDbContext(options, containerName, _columns, _migrator);
#pragma warning restore CA2000
        await context.EnsureTableExists();
        return new DbContextScope<ReadModelDbContext>(context, () => { });
    }
}
