// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueEventTypesConstraintsStorage.given;

/// <summary>
/// Sets up a <see cref="UniqueEventTypesConstraintsStorage"/> against a real MongoDB event sequence collection,
/// with a helper to append raw event documents the storage under test reads back through.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class a_unique_event_types_constraints_storage(MongoDBFixture fixture) : Cratis.Chronicle.Storage.MongoDB.Indexing.given.a_real_namespace_database(fixture)
{
    protected static readonly EventSourceId _borrower = "borrower";
    protected static readonly EventType _checkedOutEventType = new("LoanCheckedOut", EventTypeGeneration.First);
    protected static readonly EventType _returnedEventType = new("LoanReturned", EventTypeGeneration.First);
    protected const string ConstraintNameValue = "loan-open";

    /// <summary>
    /// The value <c>ConstraintBuilder.GetScope</c> writes into a participating dimension - a presence marker, never
    /// a real event source type, stream type or stream id.
    /// </summary>
    protected const string Marker = "_scoped_";

    protected UniqueEventTypesConstraintsStorage _storage;

    ulong _nextSequenceNumber;

    void Establish() => _storage = new UniqueEventTypesConstraintsStorage(_database, EventSequenceId.Log);

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

    protected Task Append(
        EventType eventType,
        EventSourceId eventSourceId,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null)
    {
        var collection = _database.GetEventSequenceCollectionFor(EventSequenceId.Log);
        var @event = new Event(
            _nextSequenceNumber++,
            CorrelationId.New(),
            [],
            [IdentityId.New()],
            eventType.Id,
            DateTimeOffset.UtcNow,
            eventSourceType ?? EventSourceType.Default,
            eventSourceId,
            eventStreamType ?? EventStreamType.All,
            eventStreamId ?? EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument>(),
            new Dictionary<string, string>(),
            []);
        return collection.InsertOneAsync(@event);
    }
}
