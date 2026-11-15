// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Hosts one event sequence table on a SQL provider and opens storages over it.
/// </summary>
public sealed class EventSequenceStorageHost : IDisposable
{
    readonly Action<DbContextOptionsBuilder<EventSequenceDbContext>> _configure;
    readonly string _tableName = $"events_{Guid.NewGuid():N}";
    readonly EventSequenceMigrator _migrator;
    readonly IIdentityStorage _identities;
    readonly IEventTypesStorage _eventTypes;
    readonly SqliteConnection? _keepAlive;
    readonly IInterceptor[] _interceptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceStorageHost"/> class and creates the table.
    /// </summary>
    /// <param name="configure">Configures the provider and caching.</param>
    /// <param name="keepAlive">An open connection keeping an in-memory database alive.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <param name="interceptors">The command interceptors.</param>
    public EventSequenceStorageHost(
        Action<DbContextOptionsBuilder<EventSequenceDbContext>> configure,
        SqliteConnection? keepAlive,
        IIdentityStorage identities,
        IEventTypesStorage eventTypes,
        params IInterceptor[] interceptors)
    {
        _configure = configure;
        _keepAlive = keepAlive;
        _identities = identities;
        _eventTypes = eventTypes;
        _interceptors = interceptors;
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
    /// <param name="interceptors">The command interceptors.</param>
    /// <returns>The host.</returns>
    public static EventSequenceStorageHost ForSqlite(IIdentityStorage identities, IEventTypesStorage eventTypes, params IInterceptor[] interceptors)
    {
        var connectionString = $"DataSource=revisions_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return new(builder => builder.UseSqlite(connectionString).EnableServiceProviderCaching(false), connection, identities, eventTypes, interceptors);
    }

    /// <summary>
    /// Creates a host on a PostgreSQL database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <returns>The host.</returns>
    /// <param name="cacheServiceProvider">Whether to use normal EF service-provider caching.</param>
    public static EventSequenceStorageHost ForPostgreSql(string connectionString, IIdentityStorage identities, IEventTypesStorage eventTypes, bool cacheServiceProvider = false) =>
        new(builder => builder.UseNpgsql(connectionString).EnableServiceProviderCaching(cacheServiceProvider), null, identities, eventTypes);

    /// <summary>
    /// Creates a host on a SQL Server database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="identities">The identity storage.</param>
    /// <param name="eventTypes">The event types storage.</param>
    /// <returns>The host.</returns>
    /// <param name="cacheServiceProvider">Whether to use normal EF service-provider caching.</param>
    public static EventSequenceStorageHost ForSqlServer(string connectionString, IIdentityStorage identities, IEventTypesStorage eventTypes, bool cacheServiceProvider = false) =>
        new(builder => builder.UseSqlServer(connectionString).EnableServiceProviderCaching(cacheServiceProvider), null, identities, eventTypes);

    /// <summary>
    /// Opens a storage instance over the hosted table.
    /// </summary>
    /// <returns>The storage.</returns>
    public IEventSequenceStorage Open()
    {
        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<string>())
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

    /// <summary>
    /// Creates a context for inspecting the hosted table.
    /// </summary>
    /// <returns>A context owned by the caller.</returns>
    public EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        _configure(builder);
        builder.AddConceptAsSupport().AddInterceptors(_interceptors);
        return new EventSequenceDbContext(builder.Options, _tableName, _migrator);
    }
}
