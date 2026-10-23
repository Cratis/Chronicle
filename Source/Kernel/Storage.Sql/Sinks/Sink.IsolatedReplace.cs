// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
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
        var id = GetIdString(key);
        var bound = checked((long)watermark.Value);
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        var rows = scope.DbContext.Entries.Where(BuildIdPredicate(id));
        var eligible = rows.Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == null || EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) <= bound);
        if (state is null)
        {
            return await RemoveIsolated(rows, eligible);
        }

        var replacement = ReplacementEntity(key, state, bound);
        if (await eligible.ExecuteUpdateAsync(setters => SetReplacementValues(setters, replacement)) > 0)
        {
            return IsolatedReplaceResult.Replaced;
        }
        if (await rows.AnyAsync())
        {
            return IsolatedReplaceResult.Superseded;
        }
        scope.DbContext.Entries.Add(replacement);
        if (await TryInsertIsolated(scope.DbContext, id))
        {
            return IsolatedReplaceResult.Replaced;
        }

        // A concurrent insert won. Re-evaluate its watermark in the database, never blindly overwrite it.
        return await eligible.ExecuteUpdateAsync(setters => SetReplacementValues(setters, replacement)) > 0
            ? IsolatedReplaceResult.Replaced : IsolatedReplaceResult.Superseded;
    }

    static async Task<IsolatedReplaceResult> RemoveIsolated(IQueryable<DynamicReadModelEntity> rows, IQueryable<DynamicReadModelEntity> eligible)
    {
        var removed = await eligible.ExecuteDeleteAsync();
        return removed > 0 || !await rows.AnyAsync() ? IsolatedReplaceResult.Removed : IsolatedReplaceResult.Superseded;
    }

    static void AddReplacementSetter<T>(UpdateSettersBuilder<DynamicReadModelEntity> setters, string name, object? value) =>
        setters.SetProperty(row => EF.Property<T>(row, name), value is null ? default : (T)value);

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
