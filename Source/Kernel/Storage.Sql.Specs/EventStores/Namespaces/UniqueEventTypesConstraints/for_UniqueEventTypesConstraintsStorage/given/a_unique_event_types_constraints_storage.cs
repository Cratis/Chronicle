// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueEventTypesConstraints.for_UniqueEventTypesConstraintsStorage.given;

/// <summary>
/// Sets up a <see cref="UniqueEventTypesConstraintsStorage"/> backed by a shared in-memory SQLite database, with a
/// helper to append raw event rows the storage under test reads back through.
/// </summary>
public class a_unique_event_types_constraints_storage : Specification
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected static readonly EventSourceId _borrower = "borrower";
    protected static readonly EventType _checkedOutEventType = new("LoanCheckedOut", EventTypeGeneration.First);
    protected static readonly EventType _returnedEventType = new("LoanReturned", EventTypeGeneration.First);
    protected const string ConstraintNameValue = "loan-open";

    /// <summary>
    /// The value <c>ConstraintBuilder.GetScope</c> writes into a participating dimension - a presence marker, never
    /// a real event source type, stream type or stream id.
    /// </summary>
    protected const string Marker = "_scoped_";

    protected SqliteConnection _connection;
    protected UniqueEventTypesConstraintsStorage _storage;

    ulong _nextSequenceNumber;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(CreateContext(), () => { })));

        _storage = new UniqueEventTypesConstraintsStorage(_eventStore, _namespace, EventSequenceId.Log, database);
    }

    void Destroy() => _connection.Dispose();

    protected static UniqueEventTypeConstraintDefinition DefinitionReleasedByReturn =>
        new(ConstraintNameValue, [_checkedOutEventType.Id], [_returnedEventType.Id]);

    /// <summary>
    /// Gets a definition scoped to the dimensions named by <paramref name="scope"/>, shaped exactly as the client
    /// builds it: <see cref="UniqueEventTypeConstraintDefinition.Scope"/> only records which dimensions participate,
    /// each marked with the presence marker <c>ConstraintBuilder.GetScope</c> writes, never a real value. Which
    /// scope an append falls into is decided by the <see cref="ResolvedConstraintScope"/> passed to
    /// <c>IsAllowedWithinScope</c>, built from the appending event's own dimension values by <see cref="ScopeFor"/> exactly as
    /// <c>UniqueEventTypeConstraintValidator</c> builds it for a real append.
    /// </summary>
    /// <param name="scope">The <see cref="ConstraintScope"/> declaring the participating dimensions.</param>
    /// <returns>A definition released by the return event and scoped as declared.</returns>
    protected static UniqueEventTypeConstraintDefinition DefinitionScopedTo(ConstraintScope scope) =>
        new(ConstraintNameValue, [_checkedOutEventType.Id], [_returnedEventType.Id], scope);

    /// <summary>
    /// Gets the scope declaring every dimension, marked the way the client marks a participating dimension.
    /// </summary>
    protected static ConstraintScope EveryDimension =>
        new((EventSourceType)Marker, (EventStreamType)Marker, (EventStreamId)Marker);

    /// <summary>
    /// Resolve the scope an append with the given dimension values carries for a definition.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> being validated against.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event being validated.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the event being validated.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the event being validated.</param>
    /// <returns>The <see cref="ResolvedConstraintScope"/> for the event being validated, or <see langword="null"/> when the definition is unscoped.</returns>
    protected static ResolvedConstraintScope? ScopeFor(
        UniqueEventTypeConstraintDefinition definition,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null) =>
        definition.Scope.ResolveFor(
            eventSourceType ?? EventSourceType.Default,
            eventStreamType ?? EventStreamType.All,
            eventStreamId ?? EventStreamId.Default);

    protected async Task Append(
        EventType eventType,
        EventSourceId eventSourceId,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null)
    {
        await using var context = CreateContext();
        context.Events.Add(new EventEntry
        {
            SequenceNumber = _nextSequenceNumber++,
            Type = eventType.Id,
            Occurred = DateTimeOffset.UtcNow,
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            EventSourceId = eventSourceId,
            EventStreamType = eventStreamType ?? EventStreamType.All,
            EventStreamId = eventStreamId ?? EventStreamId.Default
        });
        await context.SaveChangesAsync();
    }

    EventSequenceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new EventSequenceDbContext(options, $"{EventSequenceId.Log}_events", Substitute.For<IEventSequenceMigrator>());
    }
}
