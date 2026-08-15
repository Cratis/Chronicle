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
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "")
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var eventTypeIdValues = definition.EventTypeIds.Select(_ => _.Value).ToArray();
        var eventSourceIdValue = eventSourceId.Value;
        var latestRemoval = await GetLatestRemoval(scope.DbContext.Events, definition, eventSourceIdValue);

        var query = scope.DbContext.Events
            .Where(e => eventTypeIdValues.Contains(e.Type) &&
                       e.EventSourceId == eventSourceIdValue);

        if (latestRemoval is not null)
        {
            query = query.Where(e => e.SequenceNumber > latestRemoval.Value);
        }

        // Ordered so the sequence number reported back is the covered event that actually holds the cycle, rather
        // than whichever one the table happened to yield first.
        var existing = await query.OrderBy(e => e.SequenceNumber).FirstOrDefaultAsync();
        if (existing is not null)
        {
            return (false, (EventSequenceNumber)existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Find the most recent event on the event source that releases the constraint.
    /// </summary>
    /// <param name="events">The events table to read from.</param>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to read the removal events from.</param>
    /// <param name="eventSourceIdValue">The event source being answered for.</param>
    /// <returns>The sequence number the current cycle starts after, or <see langword="null"/> when nothing released it.</returns>
    /// <remarks>
    /// Any of the declared removal events ends a cycle, so the latest across all of them is the one that counts —
    /// looking at only one of them would keep answering against a cycle that another terminal fact already closed.
    /// </remarks>
    static async Task<ulong?> GetLatestRemoval(DbSet<EventEntry> events, UniqueEventTypeConstraintDefinition definition, string eventSourceIdValue)
    {
        var removalEventTypeIdValues = definition.RemovedWith.Select(_ => _.Value).ToArray();
        if (removalEventTypeIdValues.Length == 0)
        {
            return null;
        }

        var latest = await events
            .Where(e => removalEventTypeIdValues.Contains(e.Type) && e.EventSourceId == eventSourceIdValue)
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return latest?.SequenceNumber;
    }
}
