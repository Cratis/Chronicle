// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Microsoft.EntityFrameworkCore;
using Floor = Cratis.Chronicle.Concepts.Projections.Generations.Enablement.GenerationFloor;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>Commits each floor and audit revision in one transaction, guarded by a revision CAS.</summary>
/// <param name="open">Opens a store-scoped context.</param>
public sealed class GenerationFloorStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : IGenerationFloorStorage
{
    /// <inheritdoc/>
    public async Task<Floor?> Get()
    {
        await using var scope = await open();
        var row = await scope.DbContext.GenerationFloor.AsNoTracking().SingleOrDefaultAsync(_ => _.Id == 1);
        return row is null ? null : Decode(row.MinimumStorageProtocol, row.MinimumWriterProtocol, row.RequiredCapabilities, row.Incarnation, row.Revision, row.AdvancedAt, row.AdvancedBy, row.Reason);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Floor>> GetHistory()
    {
        await using var scope = await open();
        var rows = await scope.DbContext.GenerationFloorHistory.AsNoTracking().OrderBy(_ => _.Revision).ToArrayAsync();
        return rows.Select(row => Decode(row.MinimumStorageProtocol, row.MinimumWriterProtocol, row.RequiredCapabilities, row.Incarnation, row.Revision, row.AdvancedAt, row.AdvancedBy, row.Reason)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<GenerationFloorAdvanceResult> Advance(GenerationFloorAdvance advance)
    {
        await using var scope = await open();
        return await scope.DbContext.Database.CreateExecutionStrategy().ExecuteAsync(() => AdvanceOnce(advance));
    }

    static Floor Decode(int storage, int writer, string capabilities, Guid incarnation, long revision, DateTimeOffset at, string by, string reason) => new(
        new(checked((uint)storage)),
        new(checked((uint)writer)),
        capabilities.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(_ => new GenerationCapability(_)).ToArray(),
        new(incarnation),
        checked((ulong)revision),
        at,
        by,
        reason);

    async Task<GenerationFloorAdvanceResult> AdvanceOnce(GenerationFloorAdvance advance)
    {
        var result = GenerationFloorExtensions.Compare(await Get(), advance);
        if (result is not null)
        {
            return result;
        }
        var next = advance.Next();
        var capabilities = string.Join(',', next.RequiredCapabilities.Select(_ => _.Value));
        await using var scope = await open();
        var context = scope.DbContext;
        await using var transaction = await context.Database.BeginTransactionAsync();
        if (advance.ExpectedRevision == 0)
        {
            context.GenerationFloor.Add(new() { Id = 1, MinimumStorageProtocol = (int)next.MinimumStorageProtocol.Value, MinimumWriterProtocol = (int)next.MinimumWriterProtocol.Value, RequiredCapabilities = capabilities, Incarnation = next.Incarnation.Value, Revision = (long)next.Revision, AdvancedAt = next.AdvancedAt, AdvancedBy = next.AdvancedBy, Reason = next.Reason });
        }
        else if (await context.GenerationFloor.Where(_ => _.Id == 1 && _.Revision == (long)advance.ExpectedRevision).ExecuteUpdateAsync(set => set
            .SetProperty(_ => _.MinimumStorageProtocol, (int)next.MinimumStorageProtocol.Value)
            .SetProperty(_ => _.MinimumWriterProtocol, (int)next.MinimumWriterProtocol.Value)
            .SetProperty(_ => _.RequiredCapabilities, capabilities)
            .SetProperty(_ => _.Revision, (long)next.Revision)
            .SetProperty(_ => _.AdvancedAt, next.AdvancedAt)
            .SetProperty(_ => _.AdvancedBy, next.AdvancedBy)
            .SetProperty(_ => _.Reason, next.Reason)) != 1)
        {
            await transaction.RollbackAsync();
            return new GenerationFloorRevisionConflict(await Get());
        }
        context.GenerationFloorHistory.Add(new() { MinimumStorageProtocol = (int)next.MinimumStorageProtocol.Value, MinimumWriterProtocol = (int)next.MinimumWriterProtocol.Value, RequiredCapabilities = capabilities, Incarnation = next.Incarnation.Value, Revision = (long)next.Revision, AdvancedAt = next.AdvancedAt, AdvancedBy = next.AdvancedBy, Reason = next.Reason });
        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new GenerationFloorAdvanced(next);
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            await transaction.RollbackAsync();
            return new GenerationFloorRevisionConflict(await Get());
        }
    }
}
