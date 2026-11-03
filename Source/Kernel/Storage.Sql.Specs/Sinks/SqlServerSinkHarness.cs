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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Runs SQL sink specs with SQL Server's production execution strategy and table migrator.
/// </summary>
public class SqlServerSinkHarness : ISqlSinkHarness
{
    ReadModelMigrator? _migrator;

    /// <inheritdoc/>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public ReadActivity Reads { get; } = new();

    /// <summary>
    /// Gets the container supplying isolated databases.
    /// </summary>
    public SqlServerFixture? Fixture { get; init; }

    /// <summary>
    /// Gets interceptors for deterministic faults during replay promotion.
    /// </summary>
    public IEnumerable<IInterceptor> Interceptors { get; init; } = [];

    /// <summary>
    /// Gets a hook that adjusts the context options, such as installing an execution strategy that retries.
    /// </summary>
    public Action<DbContextOptionsBuilder<ReadModelDbContext>>? ConfigureOptions { get; init; }

    /// <summary>
    /// Gets the logger for index diagnostics.
    /// </summary>
    public ILogger<Sink> Logger { get; init; } = Substitute.For<ILogger<Sink>>();

    ReadModelMigrator Migrator => _migrator ??= new(
        new RecordingTableMigrator<ReadModelDbContext>(
            new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
            () => Interceptors.OfType<SqlCommandCapture>().FirstOrDefault()),
        Substitute.For<ILogger<ReadModelMigrator>>());

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        if (ConnectionString.Length == 0)
        {
            ConnectionString = Fixture!.CreateDatabase().GetAwaiter().GetResult();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>())
            .Returns(callInfo => OpenTable(callInfo.ArgAt<string>(2), callInfo.ArgAt<IReadOnlyList<ProjectedColumn>>(3)));
        database.ExistingReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(callInfo => OpenExistingTable(callInfo.ArgAt<string>(2), callInfo.ArgAt<IReadOnlyList<ProjectedColumn>>(3)));

        return new Sink(
            "test-event-store",
            "test-namespace",
            definition,
            database,
            new ExpandoObjectConverter(new TypeFormats()),
            new ReplayingTables(),
            Logger);
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

    async Task<DbContextScope<ReadModelDbContext>> OpenTable(string containerName, IReadOnlyList<ProjectedColumn> columns)
    {
        var finished = Reads.Begin();
        var context = OpenContext(containerName, columns);
        await Fixture!.Timings.Run("ef-table-ensure", context.EnsureTableExists);
        return new DbContextScope<ReadModelDbContext>(context, finished);
    }

    /// <summary>
    /// Opens a container that must already exist, as an occurrence read does: never creating the table.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="columns">The columns for this read model.</param>
    /// <returns>A <see cref="DbContextScope{TDbContext}"/> for the container, or null when it does not exist.</returns>
    async Task<DbContextScope<ReadModelDbContext>?> OpenExistingTable(string containerName, IReadOnlyList<ProjectedColumn> columns)
    {
        var finished = Reads.Begin();
        var context = OpenContext(containerName, columns);
        if (await Migrator.ProbeExistingTable(containerName, context))
        {
            return new DbContextScope<ReadModelDbContext>(context, finished);
        }

        await context.DisposeAsync();
        finished();
        return null;
    }

    ReadModelDbContext OpenContext(string containerName, IReadOnlyList<ProjectedColumn> columns)
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(ConnectionString);
        ConfigureOptions?.Invoke(builder);
        var options = builder.AddConceptAsSupport().WithoutServiceProviderCaching().AddInterceptors(Interceptors).Options;

        var fixture = Fixture!;
#pragma warning disable CA2000 // Disposed by the sink through the returned scope.
        var context = fixture.Timings.Measure("ef-context-construction", () => fixture.Track(new ReadModelDbContext(options, containerName, columns, Migrator)));
#pragma warning restore CA2000
        return context;
    }
}
