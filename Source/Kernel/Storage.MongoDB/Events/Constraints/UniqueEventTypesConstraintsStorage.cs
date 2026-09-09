// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> to use.</param>
public class UniqueEventTypesConstraintsStorage(IEventStoreNamespaceDatabase database, EventSequenceId eventSequenceId) : IUniqueEventTypesConstraintsStorage
{
    const string FlattenedScopeKeyNotSupported =
        "This storage narrows a scoped unique event type constraint by typed dimensions and cannot answer a flattened scope key. Call IsAllowedWithinScope with a ResolvedConstraintScope instead.";

    readonly IMongoCollection<Event> _collection = database.GetEventSequenceCollectionFor(eventSequenceId);

    /// <inheritdoc/>
    /// <remarks>
    /// Kept so that a caller compiled against the original interface still binds. An empty or absent key means
    /// nothing is narrowed, which is exactly an unscoped typed lookup and is answered as one. A non-empty key is
    /// refused rather than interpreted: this storage decides scope from typed dimension values, and a flattened
    /// key cannot be turned back into them without reintroducing the aliasing it exists to avoid. Refusing is a
    /// behavior change for a caller that used to pass a real key straight to this class - that call was silently
    /// answered against the wrong cycle before, so there is no correct behavior to preserve.
    /// <para>
    /// The key never reaches the message. It is built from event source type, stream type and stream id, which are
    /// caller data and potentially personal.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown when a non-empty flattened scope key is supplied.</exception>
    public Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "") =>
        string.IsNullOrEmpty(scopeKey)
            ? IsAllowedWithinScope(definition, eventSourceId)
            : throw new NotSupportedException(FlattenedScopeKeyNotSupported);

    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowedWithinScope(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, ResolvedConstraintScope? scope = null)
    {
        var forEventSource = Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId) & WithinScope(scope);

        var latestRemoval = await GetLatestRemoval(definition, forEventSource);

        var filter = forEventSource & Builders<Event>.Filter.In(_ => _.Type, definition.EventTypeIds);
        if (latestRemoval is not null)
        {
            filter &= Builders<Event>.Filter.Gt(_ => _.SequenceNumber, latestRemoval);
        }

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the collection happened to yield first. Only that one event is read back - the scope
        // is part of the filter, so the server never ships the event source's history to the client.
        var existing = await _collection.Find(filter).SortBy(_ => _.SequenceNumber).FirstOrDefaultAsync();
        if (existing is not null)
        {
            return (false, existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Build the filter narrowing candidates to the scope of the event being validated.
    /// </summary>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> of the event being validated, or <see langword="null"/> when unscoped.</param>
    /// <returns>A <see cref="FilterDefinition{TDocument}"/> narrowing to the scope, matching everything when unscoped.</returns>
    /// <remarks>
    /// Each participating dimension becomes its own equality clause on the stored event's real value, which the
    /// event sequence's compound indexes on event source id together with stream type, stream id and event source
    /// type can serve. Narrowing per dimension rather than by a flattened key is also what makes two genuinely
    /// different scopes impossible to confuse for one another.
    /// </remarks>
    static FilterDefinition<Event> WithinScope(ResolvedConstraintScope? scope)
    {
        var filter = Builders<Event>.Filter.Empty;
        if (scope is null)
        {
            return filter;
        }

        if (scope.EventSourceType is not null)
        {
            filter &= Builders<Event>.Filter.Eq(_ => _.EventSourceType, scope.EventSourceType);
        }

        if (scope.EventStreamType is not null)
        {
            filter &= Builders<Event>.Filter.Eq(_ => _.EventStreamType, scope.EventStreamType);
        }

        if (scope.EventStreamId is not null)
        {
            filter &= Builders<Event>.Filter.Eq(_ => _.EventStreamId, scope.EventStreamId);
        }

        return filter;
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <param name="forEventSource">Filter narrowing to the event source and scope being answered for.</param>
    /// <returns>The <see cref="EventSequenceNumber"/> the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// A removal in a different scope does not end this scope's cycle, which is why the scope is already part of
    /// <paramref name="forEventSource"/>.
    /// </remarks>
    async Task<EventSequenceNumber?> GetLatestRemoval(UniqueEventTypeConstraintDefinition definition, FilterDefinition<Event> forEventSource)
    {
        var removalEventTypeIds = definition.RemovedWith.ToArray();
        if (removalEventTypeIds.Length == 0)
        {
            return null;
        }

        var filter = forEventSource & Builders<Event>.Filter.In(_ => _.Type, removalEventTypeIds);
        var latest = await _collection.Find(filter).SortByDescending(_ => _.SequenceNumber).FirstOrDefaultAsync();
        return latest?.SequenceNumber;
    }
}
