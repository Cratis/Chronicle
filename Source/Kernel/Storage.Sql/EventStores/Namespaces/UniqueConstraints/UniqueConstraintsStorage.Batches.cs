// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Row = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.UniqueConstraintBatchModel.Row;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

public partial class UniqueConstraintsStorage
{
    /// <summary>
    /// The parameter budget, below SQLite's legacy limit of 999 (other providers allow more).
    /// </summary>
    internal const int MaxParametersPerStatement = 900;
    const int InsertBatchSize = MaxParametersPerStatement / 4;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)>> IsAllowedMany(
        IReadOnlyList<UniqueConstraintClaim> claims,
        UniqueConstraintDefinition definition,
        string scopeKey = "",
        CancellationToken cancellationToken = default)
    {
        if (claims.Count == 0)
        {
            return [];
        }

        var values = claims.Select(claim => claim.Value.Value).Distinct().ToArray();
        List<Row> rows;
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            rows = await ReadRows(scope.DbContext.Entries, nameof(Row.Value), values, cancellationToken);
        }
        else
        {
            await using var scope = await database.UniqueConstraintTable(eventStore, @namespace, GetTableName(definition.Name, scopeKey));
            rows = await ReadRows(scope.DbContext.Entries, nameof(Row.Value), values, cancellationToken);
        }

        var owners = rows.DistinctBy(row => row.Value).ToDictionary(row => row.Value);
        return claims.Select(claim => owners.TryGetValue(claim.Value.Value, out var owner)
            ? (owner.EventSourceId == claim.EventSourceId.Value, (EventSequenceNumber)(ulong)owner.SequenceNumber)
            : (true, EventSequenceNumber.Unavailable)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<int>> ApplyChanges(
        UniqueConstraintDefinition definition,
        IReadOnlyList<UniqueConstraintIndexChange> changes,
        CancellationToken cancellationToken = default)
    {
        var refused = new List<int>();
        foreach (var group in changes.Select((change, index) => (Change: change, Index: index)).GroupBy(item => item.Change.ScopeKey))
        {
            var scopedChanges = group.Select(item => item.Change).ToArray();
            var localRefused = await ApplyScope(definition, group.Key, scopedChanges, cancellationToken);
            var indices = group.Select(item => item.Index).ToArray();
            refused.AddRange(localRefused.Select(index => indices[index]));
        }
        return refused;
    }

    static async Task<List<Row>> ReadRows<T>(DbSet<T> entries, string property, string[] keys, CancellationToken cancellationToken)
        where T : class
    {
        var rows = new List<Row>();
        foreach (var chunk in keys.Chunk(MaxParametersPerStatement))
        {
            rows.AddRange(await entries.AsNoTracking().Where(entry => chunk.Contains(EF.Property<string>(entry, property)))
                .Select(entry => new Row(
                    EF.Property<string>(entry, nameof(Row.EventSourceId)),
                    EF.Property<string>(entry, nameof(Row.Value)),
                    EF.Property<decimal>(entry, nameof(Row.SequenceNumber)),
                    EF.Property<string?>(entry, nameof(Row.Subject))))
                .ToArrayAsync(cancellationToken));
        }
        return rows;
    }

    static Task<IReadOnlyCollection<int>> ApplyModel<T>(
        DbContext context,
        DbSet<T> entries,
        UniqueConstraintMode mode,
        UniqueConstraintIndexChange[] changes,
        Func<Row, T> createEntry,
        CancellationToken cancellationToken)
        where T : class => context.Database.CreateExecutionStrategy().ExecuteAsync<IReadOnlyCollection<int>>(async () =>
        {
            context.ChangeTracker.Clear();

            // Keep ownership reads and their replacement writes in the same serializable unit.
            // A concurrent release/reclaim must not let a stale delete remove the new owner.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var values = changes.Where(change => change.Value is not null).Select(change => change.Value!.Value).Distinct().ToArray();
            var sources = changes.Select(change => change.EventSourceId.Value).Distinct().ToArray();
            var initial = (await ReadRows(entries, nameof(Row.Value), values, cancellationToken))
                .Concat(await ReadRows(entries, nameof(Row.EventSourceId), sources, cancellationToken))
                .Distinct().ToArray();
            var model = new UniqueConstraintBatchModel(mode, initial);
            var refused = new List<int>();
            for (var index = 0; index < changes.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!model.Apply(changes[index]))
                {
                    refused.Add(index);
                }
            }
            await Flush(context, entries, mode, initial, model.Rows, createEntry, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return refused;
        });

    static async Task Flush<T>(DbContext context, DbSet<T> entries, UniqueConstraintMode mode, Row[] initial, IEnumerable<Row> final, Func<Row, T> createEntry, CancellationToken cancellationToken)
        where T : class
    {
        var keyProperty = mode == UniqueConstraintMode.PerValue ? nameof(Row.Value) : nameof(Row.EventSourceId);
        string Key(Row row) => mode == UniqueConstraintMode.PerValue ? row.Value : row.EventSourceId;
        var before = initial.ToDictionary(Key);
        var after = final.ToDictionary(Key);
        var deleted = before.Where(pair => !after.TryGetValue(pair.Key, out var row) || row != pair.Value).Select(pair => pair.Key).ToArray();
        var inserted = after.Where(pair => !before.TryGetValue(pair.Key, out var row) || row != pair.Value).Select(pair => pair.Value).ToArray();

        // Replace changed rows as well as removed rows before inserting: even cyclic value moves
        // must not collide with an intermediate state of the unique value index.
        foreach (var chunk in deleted.Chunk(MaxParametersPerStatement))
        {
            await entries.Where(entry => chunk.Contains(EF.Property<string>(entry, keyProperty))).ExecuteDeleteAsync(cancellationToken);
        }
        foreach (var chunk in inserted.Chunk(InsertBatchSize))
        {
            entries.AddRange(chunk.Select(createEntry));
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }
    }

    async Task<IReadOnlyCollection<int>> ApplyScope(UniqueConstraintDefinition definition, string scopeKey, UniqueConstraintIndexChange[] changes, CancellationToken cancellationToken)
    {
        if (definition.Mode != UniqueConstraintMode.PerValue && changes.Any(change => change.Kind == UniqueConstraintIndexChangeKind.RemoveValue))
        {
            return await UniqueConstraintIndexChanges.ApplySequentially(this, definition, changes, cancellationToken);
        }
        try
        {
            return await ApplyScopeModel(definition, scopeKey, changes, cancellationToken);
        }
        catch (DbUpdateException error) when (IsUniqueViolation(error))
        {
            // The scope's transaction has been disposed (and rolled back) before retrying.
            _telemetry.RecoveredCollision("unique-constraint-batch");
            return await UniqueConstraintIndexChanges.ApplySequentially(this, definition, changes, cancellationToken);
        }
    }

    async Task<IReadOnlyCollection<int>> ApplyScopeModel(UniqueConstraintDefinition definition, string scopeKey, UniqueConstraintIndexChange[] changes, CancellationToken cancellationToken)
    {
        if (definition.Mode == UniqueConstraintMode.PerValue)
        {
            await using var scope = await database.UniqueConstraintValuesTable(eventStore, @namespace, GetValuesTableName(definition.Name, scopeKey));
            return await ApplyModel(
                scope.DbContext,
                scope.DbContext.Entries,
                definition.Mode,
                changes,
                row => new UniqueConstraintValueEntry { EventSourceId = row.EventSourceId, Value = row.Value, SequenceNumber = row.SequenceNumber, Subject = row.Subject },
                cancellationToken);
        }
        await using var sourceScope = await database.UniqueConstraintTable(eventStore, @namespace, GetTableName(definition.Name, scopeKey));
        return await ApplyModel(
            sourceScope.DbContext,
            sourceScope.DbContext.Entries,
            definition.Mode,
            changes,
            row => new UniqueConstraintIndexEntry { EventSourceId = row.EventSourceId, Value = row.Value, SequenceNumber = row.SequenceNumber, Subject = row.Subject },
            cancellationToken);
    }
}
