// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Monads;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Sets up an <see cref="EventSequenceStorage"/> backed by a shared in-memory SQLite database.
/// A single open connection is shared across every <see cref="IDatabase.EventSequenceTable"/> scope
/// so seeded rows survive between the storage's individual unit-of-work scopes — the append path
/// opens a fresh <see cref="EventSequenceDbContext"/> per call.
/// </summary>
public class an_event_sequence_storage : Specification, IDisposable
{
    protected string _tableName = EventSequenceId.Log.Value;
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected EventSequenceId _eventSequenceId = EventSequenceId.Log;
    string? _provider;
    protected static readonly EventType _eventType = new("some-event-type", EventTypeGeneration.First);
    protected SqliteConnection _connection;
    protected string _connectionString;
    protected IEventSequenceMigrator _migrator;
    protected IDatabase _database;
    protected IIdentityStorage _identityStorage;
    protected Chronicle.Storage.EventTypes.IEventTypesStorage _eventTypesStorage;
    protected EventSequenceStorage _storage;

    void Establish()
    {
        _provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        _connectionString = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_CONNECTION_STRING") ?? $"DataSource=storage_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        if (_provider is null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        else
        {
            _tableName = $"events_{Guid.NewGuid():N}";
            _eventSequenceId = new EventSequenceId(_tableName);
        }
        _migrator = new EventSequenceMigrator(
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
            Substitute.For<ILogger<EventSequenceMigrator>>());

        using (var schemaContext = CreateContext())
        {
            schemaContext.EnsureTableExists().GetAwaiter().GetResult();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => CreateScope());

        _identityStorage = Substitute.For<IIdentityStorage>();
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);

        _eventTypesStorage = Substitute.For<Chronicle.Storage.EventTypes.IEventTypesStorage>();
        _storage = new EventSequenceStorage(
            _eventStore,
            _namespace,
            _eventSequenceId,
            _database,
            _identityStorage,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            _eventTypesStorage,
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    protected EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        switch (_provider)
        {
            case "PostgreSQL": builder.UseNpgsql(_connectionString); break;
            case "SQLServer": builder.UseSqlServer(_connectionString); break;
            default: builder.UseSqlite(_connectionString); break;
        }
        builder.AddConceptAsSupport();
        return new EventSequenceDbContext(builder.Options, _tableName, _migrator);
    }

    protected void SeedEvent(EventSequenceNumber sequenceNumber)
    {
        using var context = CreateContext();
        context.Events.Add(new EventEntry { SequenceNumber = sequenceNumber.Value });
        context.SaveChanges();
    }

    protected Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(EventSequenceNumber sequenceNumber) =>
        _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            EventSourceId.New(),
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

    protected Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(EventSequenceNumber sequenceNumber, ExpandoObject content, EventHash hash) =>
        _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            EventSourceId.New(),
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, content } },
            new Dictionary<EventTypeGeneration, EventHash> { { EventTypeGeneration.First, hash } });

    protected Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(params EventSequenceNumber[] sequenceNumbers) =>
        _storage.AppendMany(sequenceNumbers.Select(number => new EventToAppendToStorage(
            number,
            EventSourceType.Default,
            EventSourceId.New(),
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new ExpandoObject(),
            EventHash.NotSet)));

    Task<DbContextScope<EventSequenceDbContext>> CreateScope() =>
        Task.FromResult(new DbContextScope<EventSequenceDbContext>(CreateContext(), () => { }));

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
