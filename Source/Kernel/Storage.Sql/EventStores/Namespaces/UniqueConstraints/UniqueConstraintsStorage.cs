// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/> for SQL.
/// </summary>
/// <remarks>
/// This implementation uses a table-per-constraint pattern, similar to MongoDB's collection-per-constraint.
/// Each unique constraint gets its own dedicated table, allowing for efficient indexing and isolation.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for operations.</param>
public class UniqueConstraintsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database) : IUniqueConstraintsStorage
{
    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var valuesScope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            var existingValue = await valuesScope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value);
            return existingValue is null
                ? (true, EventSequenceNumber.Unavailable)
                : (existingValue.EventSourceId == eventSourceId.Value, (EventSequenceNumber)(ulong)existingValue.SequenceNumber);
        }

        var tableName = GetTableName(definition.Name, scopeKey);
        await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, tableName);

        var constraintValue = value.Value;

        // Note: Case-insensitive comparison is now handled by hashing the value with case normalization
        // before it reaches the storage layer, so we can use a simple equality check here.
        var query = scope.DbContext.Entries.Where(u => u.Value == constraintValue);

        var eventSourceIdValue = eventSourceId.Value;
        var existing = await query.FirstOrDefaultAsync();

        if (existing is not null)
        {
            if (existing.EventSourceId == eventSourceIdValue)
            {
                return (true, (EventSequenceNumber)(ulong)existing.SequenceNumber);
            }

            return (false, (EventSequenceNumber)(ulong)existing.SequenceNumber);
        }

        return (true, EventSequenceNumber.Unavailable);
    }

    /// <summary>
    /// Saves a claim using the definition's retention mode.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="sequenceNumber">The claim's sequence number.</param>
    /// <param name="value">The hashed value.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public async Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "")
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await SavePerEventSource(eventSourceId, definition.Name, sequenceNumber, value, scopeKey);
            return;
        }

        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        var existing = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value);
        if (existing is not null)
        {
            if (existing.EventSourceId != eventSourceId.Value)
            {
                throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
            }

            return;
        }

        scope.DbContext.Entries.Add(new UniqueConstraintValueEntry
        {
            Value = value.Value,
            EventSourceId = eventSourceId.Value,
            SequenceNumber = sequenceNumber.Value
        });
        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (IsUniqueViolation(error))
        {
            // A concurrent insert settles the claim through the value primary key. The same owner is idempotent.
            scope.DbContext.ChangeTracker.Clear();
            var winner = await scope.DbContext.Entries.AsNoTracking().SingleOrDefaultAsync(_ => _.Value == value.Value);
            if (winner?.EventSourceId != eventSourceId.Value)
            {
                throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
            }
        }
    }

    /// <summary>
    /// Releases every claim held by the event source in the selected scope.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "")
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await RemovePerEventSource(eventSourceId, definition.Name, scopeKey);
            return;
        }

        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        await scope.DbContext.Entries.Where(_ => _.EventSourceId == eventSourceId.Value).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Releases one value only if the event source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="value">The value to release.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "")
    {
        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        await scope.DbContext.Entries.Where(_ => _.Value == value.Value && _.EventSourceId == eventSourceId.Value).ExecuteDeleteAsync();
    }

    static bool IsUniqueViolation(DbUpdateException error) => error.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqliteException sqlite => sqlite.SqliteErrorCode == 19 && sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        SqlException sqlServer => sqlServer.Number is 2627 or 2601,
        _ => false
    };

    async Task SavePerEventSource(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey)
    {
        var tableName = GetTableName(name, scopeKey);
        await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, tableName);

        var eventSourceIdValue = eventSourceId.Value;
        var entry = await scope.DbContext.Entries
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceIdValue);

        if (entry is not null)
        {
            entry.Value = value.Value;
            entry.SequenceNumber = sequenceNumber.Value;
        }
        else
        {
            scope.DbContext.Entries.Add(new UniqueConstraintIndexEntry
            {
                EventSourceId = eventSourceId.Value,
                Value = value.Value,
                SequenceNumber = sequenceNumber.Value
            });
        }

        await scope.DbContext.SaveChangesAsync();
    }

    async Task RemovePerEventSource(EventSourceId eventSourceId, ConstraintName name, string scopeKey)
    {
        var tableName = GetTableName(name, scopeKey);
        await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, tableName);

        var eventSourceIdValue = eventSourceId.Value;
        var entry = await scope.DbContext.Entries
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceIdValue);

        if (entry is not null)
        {
            scope.DbContext.Entries.Remove(entry);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    string GetValuesTableName(ConstraintName name, string scopeKey) => string.IsNullOrEmpty(scopeKey)
        ? $"{eventSequenceId}_{name}_values_constraint"
        : $"{eventSequenceId}_{name}_{scopeKey}_values_constraint";

    string GetTableName(ConstraintName constraintName, string scopeKey = "")
    {
        return string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}_{constraintName}_constraint"
            : $"{eventSequenceId}_{constraintName}_{scopeKey}_constraint";
    }
}
