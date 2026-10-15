// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

public partial class OccurrencesStorage<TContext>
{
    /// <inheritdoc/>
    public async Task<ScheduleDefinition?> GetDefinition(ScheduleToken token)
    {
        await using var scope = await open();
        var id = StorageIdentity.Hash(token.Value);
        var row = await scope.DbContext.Set<ScheduleDefinitionEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
        return row is null ? null : JsonSerializer.Deserialize<ScheduleDefinition>(row.Data);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAddDefinition(ScheduleDefinition definition) => await TryInsert(new ScheduleDefinitionEntry
    {
        Id = StorageIdentity.Hash(definition.Token.Value), Revision = definition.Revision, Data = JsonSerializer.Serialize(definition)
    });

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDefinition(long expectedRevision, ScheduleDefinition definition)
    {
        await using var scope = await open();
        var id = StorageIdentity.Hash(definition.Token.Value);
        var data = JsonSerializer.Serialize(definition);
        return await scope.DbContext.Set<ScheduleDefinitionEntry>().Where(row => row.Id == id && row.Revision == expectedRevision)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Revision, definition.Revision).SetProperty(row => row.Data, data)) == 1;
    }

    /// <inheritdoc/>
    public async Task<ScheduleCursor?> GetCursor(ScheduleToken token)
    {
        await using var scope = await open();
        var id = StorageIdentity.Hash(token.Value);
        var row = await scope.DbContext.Set<ScheduleCursorEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
        return row is null ? null : new ScheduleCursor(row.Token, row.LastMaterializedDue, row.RegisteredRevision, row.AppliedRevision, row.Version);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAddCursor(ScheduleCursor cursor) => await TryInsert(new ScheduleCursorEntry
    {
        Id = StorageIdentity.Hash(cursor.Token.Value), Token = cursor.Token.Value, LastMaterializedDue = cursor.LastMaterializedDue,
        RegisteredRevision = cursor.RegisteredRevision, AppliedRevision = cursor.AppliedRevision, Version = cursor.Version
    });

    /// <inheritdoc/>
    public async Task RegisterRevision(ScheduleToken token, long revision)
    {
        await using var scope = await open();
        var id = StorageIdentity.Hash(token.Value);
        await scope.DbContext.Set<ScheduleCursorEntry>().Where(row => row.Id == id && row.RegisteredRevision < revision)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.RegisteredRevision, revision).SetProperty(row => row.Version, row => row.Version + 1));
    }

    /// <inheritdoc/>
    public async Task<bool> TryCommitTurn(ScheduleTurn turn)
    {
        if (turn.Next.Token != turn.Observed.Token || turn.Next.Version != turn.Observed.Version + 1 || turn.Next.RegisteredRevision != turn.Observed.RegisteredRevision || turn.Next.AppliedRevision != turn.Observed.RegisteredRevision)
        {
            return false;
        }

        try
        {
            await using var scope = await open();
            var context = scope.DbContext;
            await using var transaction = await context.Database.BeginTransactionAsync();
            var id = StorageIdentity.Hash(turn.Observed.Token.Value);
            var changed = await context.Set<ScheduleCursorEntry>().Where(row => row.Id == id && row.Version == turn.Observed.Version && row.RegisteredRevision == turn.Observed.RegisteredRevision)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.LastMaterializedDue, turn.Next.LastMaterializedDue).SetProperty(row => row.AppliedRevision, turn.Next.AppliedRevision).SetProperty(row => row.Version, turn.Next.Version));
            if (changed != 1)
            {
                return false;
            }

            foreach (var occurrence in turn.Occurrences.DistinctBy(occurrence => occurrence.Id))
            {
                var occurrenceId = StorageIdentity.Hash(occurrence.Id.Value);
                if (!await context.Set<OccurrenceEntry>().AnyAsync(row => row.Id == occurrenceId))
                {
                    var entry = OccurrenceEntry.From(occurrence);
                    entry.Schedule = id;
                    context.Set<OccurrenceEntry>().Add(entry);
                }
            }

            foreach (var range in turn.SkippedRanges.DistinctBy(range => range.Id))
            {
                var rangeId = StorageIdentity.Hash(range.Id);
                if (!await context.Set<SkippedRangeEntry>().AnyAsync(row => row.Id == rangeId))
                {
                    context.Set<SkippedRangeEntry>().Add(new SkippedRangeEntry { Id = rangeId, Token = id, Data = JsonSerializer.Serialize(range) });
                }
            }

            if (turn.DiscardPendingBeforeRevision.HasValue)
            {
                var pending = await context.Set<OccurrenceEntry>().AsNoTracking().Where(row => row.State == OccurrenceState.Pending && row.Schedule == id).ToListAsync();
                foreach (var row in pending.Where(row => row.ToOccurrence().DefinitionRevision < turn.DiscardPendingBeforeRevision))
                {
                    var epoch = row.Epoch;
                    await context.Set<OccurrenceEntry>().Where(current => current.Id == row.Id && current.Epoch == epoch && current.State == OccurrenceState.Pending)
                        .ExecuteUpdateAsync(set => set.SetProperty(current => current.State, OccurrenceState.Skipped));
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error) || StorageWriteConflicts.IsTransactionConflict(error))
        {
            // The entire transaction rolls back; the engine recomputes the bounded turn.
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SkippedRange>> GetSkippedRanges(ScheduleToken token)
    {
        await using var scope = await open();
        var id = StorageIdentity.Hash(token.Value);
        var rows = await scope.DbContext.Set<SkippedRangeEntry>().AsNoTracking().Where(row => row.Token == id).ToListAsync();
        return rows.Select(row => JsonSerializer.Deserialize<SkippedRange>(row.Data)!).ToArray();
    }

    async Task<bool> TryInsert<TEntity>(TEntity entity)
        where TEntity : class
    {
        try
        {
            await using var scope = await open();
            scope.DbContext.Set<TEntity>().Add(entity);
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            // Only these single-row inserts are attempted in this context; a primary-key race inserts nothing.
            return false;
        }
    }
}
