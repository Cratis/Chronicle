// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
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
    public async Task ClearValues(UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await using var sourceScope = await database.UniqueConstraintTable(eventStore, @namespace, GetTableName(definition.Name, scopeKey));
            await sourceScope.DbContext.Entries.ExecuteDeleteAsync(cancellationToken);
            return;
        }

        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        await scope.DbContext.Entries.ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UniqueConstraintValue>> GetFor(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            var values = await scope.DbContext.Entries.AsNoTracking().Where(row => row.EventSourceId == eventSourceId.Value).Select(row => row.Value).ToArrayAsync(cancellationToken);
            return values.Select(value => new UniqueConstraintValue(value)).ToArray();
        }

        await using var sourceScope = await database.UniqueConstraintTable(eventStore, @namespace, GetTableName(definition.Name, scopeKey));
        var sourceValues = await sourceScope.DbContext.Entries.AsNoTracking().Where(row => row.EventSourceId == eventSourceId.Value).Select(row => row.Value).ToArrayAsync(cancellationToken);
        return sourceValues.Select(value => new UniqueConstraintValue(value)).ToArray();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<UniqueConstraintValue> GetForStream(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            await foreach (var value in scope.DbContext.Entries.AsNoTracking().Where(row => row.EventSourceId == eventSourceId.Value).Select(row => row.Value).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return new UniqueConstraintValue(value);
            }
            yield break;
        }

        await using var sourceScope = await database.UniqueConstraintTable(eventStore, @namespace, GetTableName(definition.Name, scopeKey));
        await foreach (var value in sourceScope.DbContext.Entries.AsNoTracking().Where(row => row.EventSourceId == eventSourceId.Value).Select(row => row.Value).AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return new UniqueConstraintValue(value);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var valuesScope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            var existingValue = await valuesScope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value, cancellationToken);
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
        var existing = await query.FirstOrDefaultAsync(cancellationToken);

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
    /// <param name="subject">The subject whose personal data the value derives from, or null.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">Another event source owns the value.</exception>
    public async Task Save(EventSourceId eventSourceId, UniqueConstraintDefinition definition, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey = "", Subject? subject = default, CancellationToken cancellationToken = default)
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await SavePerEventSource(eventSourceId, definition.Name, sequenceNumber, value, scopeKey, subject?.Value, cancellationToken);
            return;
        }

        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        var existing = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value, cancellationToken);
        if (existing is not null)
        {
            if (existing.EventSourceId != eventSourceId.Value)
            {
                throw new DuplicateUniqueConstraintValue(definition.Name, eventSourceId);
            }

            if (existing.Subject != subject?.Value)
            {
                var subjectValue = subject?.Value;
                await scope.DbContext.Entries
                    .Where(_ => _.Value == value.Value && _.EventSourceId == eventSourceId.Value)
                    .ExecuteUpdateAsync(_ => _.SetProperty(row => row.Subject, subjectValue), cancellationToken);
            }

            return;
        }

        scope.DbContext.Entries.Add(new UniqueConstraintValueEntry
        {
            Value = value.Value,
            EventSourceId = eventSourceId.Value,
            SequenceNumber = sequenceNumber.Value,
            Subject = subject?.Value
        });
        try
        {
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (IsUniqueViolation(error))
        {
            // A concurrent insert settles the claim through the value primary key. The same owner is idempotent.
            scope.DbContext.ChangeTracker.Clear();
            var winner = await scope.DbContext.Entries.AsNoTracking().SingleOrDefaultAsync(_ => _.Value == value.Value, cancellationToken);
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
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Remove(EventSourceId eventSourceId, UniqueConstraintDefinition definition, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            await RemovePerEventSource(eventSourceId, definition.Name, scopeKey, cancellationToken);
            return;
        }

        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        await scope.DbContext.Entries.Where(_ => _.EventSourceId == eventSourceId.Value).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Releases one value only if the event source owns it.
    /// </summary>
    /// <param name="eventSourceId">The owner.</param>
    /// <param name="definition">The constraint.</param>
    /// <param name="value">The value to release.</param>
    /// <param name="scopeKey">The resolved scope.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RemoveValue(EventSourceId eventSourceId, UniqueConstraintDefinition definition, UniqueConstraintValue value, string scopeKey = "", CancellationToken cancellationToken = default)
    {
        await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
        await scope.DbContext.Entries.Where(_ => _.Value == value.Value && _.EventSourceId == eventSourceId.Value).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveForSubject(Subject subject, CancellationToken cancellationToken = default)
    {
        (string[] PerEventSource, string[] PerValue) tables;
        await using (var namespaceScope = await database.Namespace(eventStore, @namespace))
        {
            tables = await UniqueConstraintTables.For(namespaceScope.DbContext, eventSequenceId, cancellationToken);
        }

        foreach (var table in tables.PerEventSource)
        {
            await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, table);
            await scope.DbContext.Entries.Where(_ => _.Subject == subject.Value).ExecuteDeleteAsync(cancellationToken);
        }

        foreach (var table in tables.PerValue)
        {
            await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, table);
            await scope.DbContext.Entries.Where(_ => _.Subject == subject.Value).ExecuteDeleteAsync(cancellationToken);
        }
    }

    static bool IsUniqueViolation(DbUpdateException error) => error.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqliteException sqlite => sqlite.SqliteErrorCode == 19 && sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        SqlException sqlServer => sqlServer.Number is 2627 or 2601,
        _ => false
    };

    async Task SavePerEventSource(EventSourceId eventSourceId, ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value, string scopeKey, string? subject, CancellationToken cancellationToken)
    {
        var tableName = GetTableName(name, scopeKey);
        await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, tableName);

        var eventSourceIdValue = eventSourceId.Value;
        var entry = await scope.DbContext.Entries
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceIdValue, cancellationToken);

        if (entry is not null)
        {
            entry.Value = value.Value;
            entry.SequenceNumber = sequenceNumber.Value;
            entry.Subject = subject;
        }
        else
        {
            scope.DbContext.Entries.Add(new UniqueConstraintIndexEntry
            {
                EventSourceId = eventSourceId.Value,
                Value = value.Value,
                SequenceNumber = sequenceNumber.Value,
                Subject = subject
            });
        }

        try
        {
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (IsUniqueViolation(error))
        {
            scope.DbContext.ChangeTracker.Clear();
            var owner = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value, cancellationToken);
            if (owner is not null && owner.EventSourceId != eventSourceIdValue)
            {
                throw new DuplicateUniqueConstraintValue(name, eventSourceId);
            }

            // A concurrent insert of this source won the primary-key race. Retry as an update,
            // even if it claimed a different value; the unique value index still settles the move.
            var winner = await scope.DbContext.Entries.FirstOrDefaultAsync(_ => _.EventSourceId == eventSourceIdValue, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            winner.Value = value.Value;
            winner.SequenceNumber = sequenceNumber.Value;
            winner.Subject = subject;
            try
            {
                await scope.DbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException retryError) when (IsUniqueViolation(retryError))
            {
                scope.DbContext.ChangeTracker.Clear();
                var retryOwner = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(_ => _.Value == value.Value, cancellationToken);
                if (retryOwner is not null && retryOwner.EventSourceId != eventSourceIdValue)
                {
                    throw new DuplicateUniqueConstraintValue(name, eventSourceId);
                }

                throw;
            }
        }
    }

    async Task RemovePerEventSource(EventSourceId eventSourceId, ConstraintName name, string scopeKey, CancellationToken cancellationToken)
    {
        var tableName = GetTableName(name, scopeKey);
        await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, tableName);

        var eventSourceIdValue = eventSourceId.Value;
        var entry = await scope.DbContext.Entries
            .FirstOrDefaultAsync(u => u.EventSourceId == eventSourceIdValue, cancellationToken);

        if (entry is not null)
        {
            scope.DbContext.Entries.Remove(entry);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    string GetValuesTableName(ConstraintName name, string scopeKey) => string.IsNullOrEmpty(scopeKey)
        ? $"{eventSequenceId}_{name}_constraint_values"
        : $"{eventSequenceId}_{name}_{scopeKey}_constraint_values";

    string GetTableName(ConstraintName constraintName, string scopeKey = "")
    {
        return string.IsNullOrEmpty(scopeKey)
            ? $"{eventSequenceId}_{constraintName}_constraint"
            : $"{eventSequenceId}_{constraintName}_{scopeKey}_constraint";
    }
}
