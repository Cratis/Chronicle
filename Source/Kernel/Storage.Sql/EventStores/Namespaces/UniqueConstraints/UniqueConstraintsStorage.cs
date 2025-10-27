// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for operations.</param>
public class UniqueConstraintsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database) : IUniqueConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var query = scope.DbContext.UniqueConstraintIndexes
            .Where(u => u.ConstraintName == definition.Name.Value && u.Value == value.Value);

        // Apply case-insensitive comparison if needed
        if (definition.IgnoreCasing)
        {
            // Note: For case-insensitive comparison in SQL, we rely on the database collation
            // This can be configured at the database level or by using specific collation in queries
        }

        var existing = await query.FirstOrDefaultAsync();

        if (existing is not null)
        {
            if (existing.EventSourceId == eventSourceId.Value)
            {
                return (true, (EventSequenceNumber)existing.SequenceNumber);
            }

            return (false, (EventSequenceNumber)existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public async Task Save(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.UniqueConstraintIndexes
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceId.Value && u.ConstraintName == name.Value);

        if (entry is not null)
        {
            // Update existing entry
            entry.Value = value.Value;
            entry.SequenceNumber = sequenceNumber.Value;
        }
        else
        {
            // Create new entry
            scope.DbContext.UniqueConstraintIndexes.Add(new UniqueConstraintIndexEntry
            {
                EventSourceId = eventSourceId.Value,
                ConstraintName = name.Value,
                Value = value.Value,
                SequenceNumber = sequenceNumber.Value
            });
        }

        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Remove(EventSourceId eventSourceId, ConstraintName name)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var entry = await scope.DbContext.UniqueConstraintIndexes
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceId.Value && u.ConstraintName == name.Value);

        if (entry is not null)
        {
            scope.DbContext.UniqueConstraintIndexes.Remove(entry);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
