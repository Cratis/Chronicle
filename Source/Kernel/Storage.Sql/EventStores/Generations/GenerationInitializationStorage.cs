// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>
/// Stores immutable receipts and retained blockers with conditional updates.
/// </summary>
/// <param name="open">Opens a store context.</param>
public sealed class GenerationInitializationStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : IGenerationInitializationStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationInitializationReceipt>> GetReceipts()
    {
        await using var scope = await open();
        var rows = await scope.DbContext.GenerationInitializationReceipts.AsNoTracking().ToArrayAsync();
        return rows.Select(row => new GenerationInitializationReceipt(new(row.EventStore), (GenerationInitializationStep)row.Step, row.Subject, row.Evidence, new(row.Incarnation), checked((ulong)row.FloorRevision), row.RecordedAt)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> TryRecord(GenerationInitializationReceipt receipt)
    {
        await using var scope = await open();
        scope.DbContext.GenerationInitializationReceipts.Add(new() { Step = (int)receipt.Step, Subject = receipt.Subject, EventStore = receipt.EventStore.Value, Evidence = receipt.Evidence, Incarnation = receipt.Incarnation.Value, FloorRevision = checked((long)receipt.FloorRevision), RecordedAt = receipt.RecordedAt });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationInitializationBlocker>> GetBlockers()
    {
        await using var scope = await open();
        var rows = await scope.DbContext.GenerationInitializationBlockers.AsNoTracking().ToArrayAsync();
        return rows.Select(row => new GenerationInitializationBlocker(new(row.EventStore), (GenerationInitializationBlockerKind)row.Kind, row.Subject, row.Detail, row.RaisedAt, row.ResolvedAt)).ToArray();
    }

    /// <inheritdoc/>
    public async Task RecordBlocker(GenerationInitializationBlocker blocker)
    {
        await using var scope = await open();
        var rows = scope.DbContext.GenerationInitializationBlockers.Where(row => row.Kind == (int)blocker.Kind && row.Subject == blocker.Subject);
        if (await rows.Where(row => row.ResolvedAt == null).ExecuteUpdateAsync(set => set.SetProperty(row => row.Detail, blocker.Detail)) == 1)
        {
            return;
        }
        if (await rows.Where(row => row.ResolvedAt != null).ExecuteUpdateAsync(set => set.SetProperty(row => row.Detail, blocker.Detail).SetProperty(row => row.RaisedAt, blocker.RaisedAt).SetProperty(row => row.ResolvedAt, (DateTimeOffset?)null)) == 1)
        {
            return;
        }
        scope.DbContext.GenerationInitializationBlockers.Add(new() { Kind = (int)blocker.Kind, Subject = blocker.Subject, EventStore = blocker.EventStore.Value, Detail = blocker.Detail, RaisedAt = blocker.RaisedAt, ResolvedAt = null });
        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            await RecordBlocker(blocker);
        }
    }

    /// <inheritdoc/>
    public async Task ResolveBlocker(GenerationInitializationBlockerKind kind, string subject, DateTimeOffset at)
    {
        await using var scope = await open();
        await scope.DbContext.GenerationInitializationBlockers.Where(row => row.Kind == (int)kind && row.Subject == subject && row.ResolvedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ResolvedAt, at));
    }
}
