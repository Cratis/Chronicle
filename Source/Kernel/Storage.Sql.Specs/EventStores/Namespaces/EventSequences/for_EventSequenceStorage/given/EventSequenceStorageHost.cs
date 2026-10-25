// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Hosts one event sequence table on a SQL provider and opens any number of storages over it.
/// </summary>
public sealed class EventSequenceStorageHost : IDisposable
{
    readonly Action<DbContextOptionsBuilder<EventSequenceDbContext>, string> _useProvider;
    readonly string _connectionString;
    readonly string _tableName = $"events_{Guid.NewGuid():N}";
    readonly EventSequenceMigrator _migrator;
    readonly IIdentityStorage _identities;
    readonly IEventTypesStorage _eventTypes;
    readonly SqliteConnection? _keepAlive;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceStorageHost"/> class and creates the table.
    /// </summary>
    /// <param name="useProvider">Configures the provider on the context options.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="keepAlive">An open connection that keeps an in-memory database alive, if any.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    public EventSequenceStorageHost(
        Action<DbContextOptionsBuilder<EventSequenceDbContext>, string> useProvider,
        string connectionString,
        SqliteConnection? keepAlive,
        IIdentityStorage identities,
        IEventTypesStorage eventTypes)
    {
        _useProvider = useProvider;
        _connectionString = connectionString;
        _keepAlive = keepAlive;
        _identities = identities;
        _eventTypes = eventTypes;
        _migrator = new EventSequenceMigrator(
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
            Substitute.For<ILogger<EventSequenceMigrator>>());
        using var context = CreateContext();
        context.EnsureTableExists().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a SQLite host on a shared-cache in-memory database.
    /// </summary>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <returns>The host.</returns>
    public static EventSequenceStorageHost ForSqlite(IIdentityStorage identities, IEventTypesStorage eventTypes)
    {
        var connectionString = $"DataSource=revisions_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return new((builder, connectionString) => builder.UseSqlite(connectionString), connectionString, connection, identities, eventTypes);
    }

    /// <summary>
    /// Creates a host on a PostgreSQL database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <returns>The host.</returns>
    public static EventSequenceStorageHost ForPostgreSql(string connectionString, IIdentityStorage identities, IEventTypesStorage eventTypes) =>
        new((builder, connectionString) => builder.UseNpgsql(connectionString), connectionString, null, identities, eventTypes);

    /// <summary>
    /// Creates a host on a SQL Server database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <returns>The host.</returns>
    public static EventSequenceStorageHost ForSqlServer(string connectionString, IIdentityStorage identities, IEventTypesStorage eventTypes) =>
        new((builder, connectionString) => builder.UseSqlServer(connectionString), connectionString, null, identities, eventTypes);

    /// <summary>
    /// Opens a new storage instance over the hosted table.
    /// </summary>
    /// <returns>The storage.</returns>
    public IEventSequenceStorage Open()
    {
        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(CreateContext(), () => { })));
        return new EventSequenceStorage(
            "test-store",
            "test-namespace",
            new EventSequenceId(_tableName),
            database,
            _identities,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            _eventTypes,
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    /// <inheritdoc/>
    public void Dispose() => _keepAlive?.Dispose();

    EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        _useProvider(builder, _connectionString);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new EventSequenceDbContext(builder.Options, _tableName, _migrator);
    }
}
