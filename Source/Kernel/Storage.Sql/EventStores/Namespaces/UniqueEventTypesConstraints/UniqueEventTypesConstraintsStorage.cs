// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
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
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

        var existing = await scope.DbContext.Events
            .Where(e => e.Type == eventTypeId &&
                       e.EventSourceId == eventSourceId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return (false, (EventSequenceNumber)existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }
}
