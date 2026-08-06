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

    static async Task<ulong?> GetLatestRemoval(DbSet<EventEntry> events, UniqueEventTypeConstraintDefinition definition, string eventSourceIdValue)
    {
        if (definition.RemovedWith is null)
        {
            return null;
        }

        var removalEventTypeIdValue = definition.RemovedWith.Value;
        var latest = await events
            .Where(e => e.Type == removalEventTypeIdValue && e.EventSourceId == eventSourceIdValue)
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        return latest?.SequenceNumber;
    }
}
