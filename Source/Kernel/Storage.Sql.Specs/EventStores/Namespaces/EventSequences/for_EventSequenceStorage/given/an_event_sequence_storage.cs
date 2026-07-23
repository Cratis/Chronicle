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
    protected static readonly string _tableName = "event-sequence";
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected static readonly EventSequenceId _eventSequenceId = EventSequenceId.Log;
    protected static readonly EventType _eventType = new("some-event-type", EventTypeGeneration.First);
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected IIdentityStorage _identityStorage;
    protected EventSequenceStorage _storage;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => CreateScope());

        _identityStorage = Substitute.For<IIdentityStorage>();
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);

        _storage = new EventSequenceStorage(
            _eventStore,
            _namespace,
            _eventSequenceId,
            _database,
            _identityStorage,
            Substitute.For<ILogger<EventSequenceStorage>>());
    }

    protected EventSequenceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new EventSequenceDbContext(options, _tableName, Substitute.For<IEventSequenceMigrator>());
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
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
