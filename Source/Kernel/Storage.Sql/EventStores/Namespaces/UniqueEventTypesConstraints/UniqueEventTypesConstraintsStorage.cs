// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueEventTypesConstraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueEventTypesConstraintsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for operations.</param>
public class UniqueEventTypesConstraintsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database) : IUniqueEventTypesConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, ResolvedConstraintScope? scope = null)
    {
        await using var tableScope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var eventTypeIdValues = definition.EventTypeIds.Select(_ => _.Value).ToArray();
        var eventSourceIdValue = eventSourceId.Value;
        var forEventSource = WithinScope(tableScope.DbContext.Events.Where(e => e.EventSourceId == eventSourceIdValue), scope);
        var latestRemoval = await GetLatestRemoval(forEventSource, definition);

        var query = forEventSource.Where(e => eventTypeIdValues.Contains(e.Type));

        if (latestRemoval is not null)
        {
            query = query.Where(e => e.SequenceNumber > latestRemoval.Value);
        }

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the table happened to yield first. Only that one row is materialized - the scope is
        // part of the query, so the event source's history is never pulled into memory to be filtered.
        var existing = await query.OrderBy(e => e.SequenceNumber).FirstOrDefaultAsync();
        if (existing is not null)
        {
            return (false, (EventSequenceNumber)existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Narrow a query to the scope of the event being validated.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> of the event being validated, or <see langword="null"/> when unscoped.</param>
    /// <returns>The query narrowed to the scope, unchanged when unscoped.</returns>
    /// <remarks>
    /// Each participating dimension becomes its own equality predicate on the stored row's real value, translated
    /// into SQL rather than evaluated in memory. Narrowing per dimension rather than by a flattened key is also
    /// what makes two genuinely different scopes impossible to confuse for one another.
    /// </remarks>
    static IQueryable<EventEntry> WithinScope(IQueryable<EventEntry> query, ResolvedConstraintScope? scope)
    {
        if (scope is null)
        {
            return query;
        }

        if (scope.EventSourceType is not null)
        {
            var eventSourceTypeValue = scope.EventSourceType.Value;
            query = query.Where(e => e.EventSourceType == eventSourceTypeValue);
        }

        if (scope.EventStreamType is not null)
        {
            var eventStreamTypeValue = scope.EventStreamType.Value;
            query = query.Where(e => e.EventStreamType == eventStreamTypeValue);
        }

        if (scope.EventStreamId is not null)
        {
            var eventStreamIdValue = scope.EventStreamId.Value;
            query = query.Where(e => e.EventStreamId == eventStreamIdValue);
        }

        return query;
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="forEventSource">Query already narrowed to the event source and scope being answered for.</param>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <returns>The sequence number the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// A removal in a different scope does not end this scope's cycle, which is why the scope is already part of
    /// <paramref name="forEventSource"/>.
    /// </remarks>
    static async Task<ulong?> GetLatestRemoval(IQueryable<EventEntry> forEventSource, UniqueEventTypeConstraintDefinition definition)
    {
        var removalEventTypeIdValues = definition.RemovedWith.Select(_ => _.Value).ToArray();
        if (removalEventTypeIdValues.Length == 0)
        {
            return null;
        }

        var latest = await forEventSource
            .Where(e => removalEventTypeIdValues.Contains(e.Type))
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return latest?.SequenceNumber;
    }
}
