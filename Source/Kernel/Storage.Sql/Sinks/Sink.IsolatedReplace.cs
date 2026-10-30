// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Dynamic;
using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnDocuments;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public bool SupportsIsolatedReplace => true;

    /// <inheritdoc/>
    public async Task<IsolatedReplaceResult> ReplaceIsolated(Key key, ExpandoObject? state, EventSequenceNumber watermark)
    {
        try
        {
            return await ReplaceIsolatedAttempt(key, state, watermark);
        }
        catch (Exception exception) when (StorageWriteConflicts.IsUnique(exception) || StorageWriteConflicts.IsTransactionConflict(exception))
        {
            // Retry a refused transaction once with a fresh scope/snapshot. Further contention is an
            // error, not evidence that a newer watermark superseded this replacement.
            return await ReplaceIsolatedAttempt(key, state, watermark);
        }
    }

    static async Task<IsolatedReplaceResult> RemoveIsolated(IQueryable<DynamicReadModelEntity> rows, IQueryable<DynamicReadModelEntity> eligible)
    {
        var removed = await eligible.ExecuteDeleteAsync();
        return removed > 0 || !await rows.AnyAsync() ? IsolatedReplaceResult.Removed : IsolatedReplaceResult.Superseded;
    }

    static void AddReplacementSetter<T>(UpdateSettersBuilder<DynamicReadModelEntity> setters, string name, object? value) =>
        setters.SetProperty(row => EF.Property<T>(row, name), value is null ? default : (T)value);

    async Task<IsolatedReplaceResult> ReplaceIsolatedAttempt(Key key, ExpandoObject? state, EventSequenceNumber watermark)
    {
        var bound = checked((long)watermark.Value);
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        return await scope.DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            scope.DbContext.ChangeTracker.Clear();

            // Serialize both existing rows and missing-key decisions. A competing insert/update/delete
            // either precedes this snapshot, waits for it, or forces a serialization/deadlock retry.
            await using var transaction = await scope.DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var rows = scope.DbContext.Entries.Where(BuildIdPredicate(GetIdString(key)));
            var eligible = rows.Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == null || EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) <= bound);
            var result = state is null
                ? await RemoveIsolated(rows, eligible)
                : await WriteIsolated(scope.DbContext, rows, eligible, ReplacementEntity(key, state, bound));
            await transaction.CommitAsync();
            return result;
        });
    }

    async Task<IsolatedReplaceResult> WriteIsolated(ReadModelDbContext context, IQueryable<DynamicReadModelEntity> rows, IQueryable<DynamicReadModelEntity> eligible, DynamicReadModelEntity replacement)
    {
        if (await eligible.ExecuteUpdateAsync(setters => SetReplacementValues(setters, replacement)) > 0)
        {
            return IsolatedReplaceResult.Replaced;
        }
        if (await rows.AnyAsync())
        {
            return IsolatedReplaceResult.Superseded;
        }
        context.Entries.Add(replacement);
        await context.SaveChangesAsync();
        return IsolatedReplaceResult.Replaced;
    }

    DynamicReadModelEntity ReplacementEntity(Key key, ExpandoObject state, long watermark)
    {
        var replacement = new DynamicReadModelEntity();
        SeedDefaults(replacement);
        var values = (IDictionary<string, object?>)state;
        foreach (var column in _columns)
        {
            if (column.IsKey)
            {
                replacement[column.Name] = CoerceForColumn(column, key.Value is ExpandoObject ? GetIdString(key) : key.Value);
            }
            else if (values.TryGetValue(column.Name, out var value))
            {
                replacement[column.Name] = column.IsJson ? SerializeJsonColumn(value, column) : CoerceForColumn(column, value);
            }
        }
        replacement[WellKnownProperties.LastHandledEventSequenceNumber] = watermark;
        replacement[WellKnownProperties.ReadModelInstanceInitialized] = true;
        return replacement;
    }

    void SetReplacementValues(UpdateSettersBuilder<DynamicReadModelEntity> setters, DynamicReadModelEntity replacement)
    {
        foreach (var column in _columns.Where(column => !column.IsKey))
        {
            replacement.TryGetValue(column.Name, out var value);
            typeof(Sink).GetMethod(nameof(AddReplacementSetter), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(column.ClrType)
                .Invoke(null, [setters, column.Name, value]);
        }
    }

    async Task<bool> TryInsertIsolated(ReadModelDbContext context, string id)
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            context.ChangeTracker.Clear();
            if (!await context.Entries.AnyAsync(BuildIdPredicate(id)))
            {
                throw;
            }
            return false;
        }
    }
}
