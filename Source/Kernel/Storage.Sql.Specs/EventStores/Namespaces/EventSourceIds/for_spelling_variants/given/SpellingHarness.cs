// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.given;

/// <summary>
/// Hosts event sequence and closed stream storage on one database for the configured provider.
/// </summary>
/// <param name="provider">The provider name, either sql_server or sqlite.</param>
/// <param name="createDatabase">Creates the database and returns its connection string.</param>
/// <param name="dropDatabase">Drops the database.</param>
public sealed class SpellingHarness(string provider, Func<Task<string>> createDatabase, Func<string, Task> dropDatabase) : IAsyncDisposable
{
    static readonly EventStoreName _eventStore = "spelling-store";
    static readonly EventStoreNamespaceName _namespace = "spelling-namespace";
    static readonly EventType _eventType = new("spelling-event-type", EventTypeGeneration.First);
    readonly string _tableName = $"events_{Guid.NewGuid():N}";
    readonly IEventSequenceMigrator _migrator = new EventSequenceMigrator(
        new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
        Substitute.For<ILogger<EventSequenceMigrator>>());
    string _connectionString;
    ulong _next;

    public CommandCounter Counter { get; } = new();

    public EventSequenceStorage Events { get; private set; }
    public ClosedStreamsConstraintStorage ClosedStreams { get; private set; }

    public async Task Initialize()
    {
        _connectionString = await createDatabase();
        await using (var namespaceContext = NamespaceContext())
        {
            await namespaceContext.Database.MigrateAsync();
        }
        await using (var eventsContext = EventsContext())
        {
            await eventsContext.EnsureTableExists();
        }

        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(EventsContext(), () => { })));
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(NamespaceContext(), () => { })));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        var sequenceId = new EventSequenceId(_tableName);
        Events = new(
            _eventStore,
            _namespace,
            sequenceId,
            database,
            identities,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            Substitute.For<Chronicle.Storage.EventTypes.IEventTypesStorage>(),
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
        ClosedStreams = new(_eventStore, _namespace, sequenceId, database);
    }

    public async Task<ulong> Append(EventSourceId eventSourceId)
    {
        var sequenceNumber = _next++;
        await Events.Append(
            sequenceNumber,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
        return sequenceNumber;
    }

    public async Task<ulong[]> Read(EventSourceId eventSourceId)
    {
        using var cursor = await Events.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId);
        var numbers = new List<ulong>();
        while (await cursor.MoveNext())
        {
            numbers.AddRange(cursor.Current.Select(_ => _.Context.SequenceNumber.Value));
        }
        return [.. numbers.Order()];
    }

    public async ValueTask DisposeAsync() => await dropDatabase(_connectionString);

    NamespaceDbContext NamespaceContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>().AddConceptAsSupport().WithoutServiceProviderCaching();
        Configure(builder);
        return new(builder.Options);
    }

    EventSequenceDbContext EventsContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>().AddConceptAsSupport().WithoutServiceProviderCaching();
        Configure(builder);
        builder.AddInterceptors(Counter);
        return new(builder.Options, _tableName, _migrator);
    }

    void Configure(DbContextOptionsBuilder builder)
    {
        if (provider == "sql_server")
        {
            builder.UseSqlServer(_connectionString);
        }
        else
        {
            builder.UseSqlite(_connectionString);
        }
    }
}
