// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable SA1202 // Private helpers grouped next to the methods they back.
#pragma warning disable SA1204 // Static helpers grouped next to the methods they back.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that promotes a rebuilt replay table to the primary table.
/// </summary>
public partial class Sink
{
    async Task PerformRenameSwap(ReplayContext context)
    {
        var revertName = context.RevertContainerName.Value;
        var backup = BackupHandleFor(revertName);
        var live = await _tables.Find(_live) ?? throw new ReadModelTableNameBindingFailure(_live.Owner, _tableName, "the live table has no binding to promote into");

        // The swap renames tables, so it connects as the schema identity when connection resolution distinguishes one.
        await using var scope = await _tables.Open(_replay, _columns, ConnectionPurpose.Schema);
        var replayPhysical = scope.DbContext.TableName;
        await scope.DbContext.EnsureReplayPromotions();
        var replayHasRows = await scope.DbContext.Entries.AsNoTracking().AnyAsync();

        // An empty replay does not publish or touch a backup. Otherwise reserve its name before anything moves.
        var backupBinding = replayHasRows ? await _tables.Reserve(backup) : null;
        var backupPhysical = backupBinding?.PhysicalName ?? string.Empty;
        var marker = new ReplayPromotion(ReplayPromotion.IdentifierFor(_tableName, revertName), _tableName, revertName, backupPhysical);
        var promotion = new ReplayTablePromotion(replayPhysical, live.PhysicalName, backupPhysical, replayHasRows, marker, backup, backupBinding?.State == ReadModelTableBindingState.Ready);

        // Publish both renames, their constraint names and the ledger transitions in one transaction. Otherwise a concurrent
        // read can see the live table missing and recreate it, making replay -> live collide. A database transaction also
        // protects readers on other silos and restores the previous tables if any step of the promotion fails.
        var strategy = scope.DbContext.Database.CreateExecutionStrategy();
        await DatabaseSchemaLock.Run(scope.DbContext, () => strategy.ExecuteInTransactionAsync(
            async () =>
            {
                // Verification can fail to observe a commit after its acknowledgment is lost. Every attempt must
                // independently recognize that this replay already published, even if a late writer
                // has recreated and populated the replay table.
                if (await SwapHasCommitted(scope.DbContext, marker))
                {
                    return;
                }

                await PromoteReplayTable(scope, promotion);
            },

            () => SwapHasCommitted(scope.DbContext, marker)));
    }

    /// <summary>
    /// Renames the replay table into place inside the promotion transaction.
    /// </summary>
    /// <param name="scope">The <see cref="DbContextScope{TContext}"/> bound to the replay table.</param>
    /// <param name="promotion">The <see cref="ReplayTablePromotion"/> describing the tables involved.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ReplayTableBecameEmpty">The rebuilt table lost its rows before the promotion.</exception>
    /// <exception cref="ReadModelTableNameBindingFailure">The backup binding changed during promotion.</exception>
    async Task PromoteReplayTable(DbContextScope<ReadModelDbContext> scope, ReplayTablePromotion promotion)
    {
        var (replayPhysical, livePhysical, backupPhysical, replayHasRows, marker, backup, backupWasReady) = promotion;
        if (!replayHasRows)
        {
            // No backup is touched on this path, so its ownership cannot block an empty replay.
            await ExecuteDdl(scope, BuildDropSql(scope, replayPhysical));
            await ReopenReplayTable(scope, marker.RevertContainerName);
            return;
        }

        // Re-check on every attempt before touching the backup. A reader can recreate an empty
        // replay table after a committed swap; it must never replace the rebuilt live table.
        if (!await scope.DbContext.Entries.AsNoTracking().AnyAsync())
        {
            throw new ReplayTableBecameEmpty(replayPhysical);
        }

        var databaseType = scope.DbContext.Database.GetDatabaseType();
        await ExecuteDdl(scope, BuildDropSql(scope, backupPhysical));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, livePhysical, backupPhysical));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, replayPhysical, livePhysical));

        // Constraint names must move in the same transaction, before the next replay is created.
        await PrimaryKeyConstraints.NameAfterTable(scope, backupPhysical);
        await PrimaryKeyConstraints.NameAfterTable(scope, livePhysical);
        await ReadModelIndexes.NameAfterTable(scope.DbContext, backupPhysical);
        await ReadModelIndexes.NameAfterTable(scope.DbContext, livePhysical);
        await RecordPromotion(scope.DbContext, marker);

        // The backup now exists under its bound name, and the replay name is free for the next replay.
        if (!backupWasReady && !await _tables.Transition(backup, ReadModelTableBindingState.Reserved, ReadModelTableBindingState.Ready, scope.DbContext))
        {
            throw new ReadModelTableNameBindingFailure(backup.Owner, backupPhysical, "the backup binding changed during the promotion");
        }

        await ReopenReplayTable(scope, marker.RevertContainerName);
    }

    async Task ReopenReplayTable(DbContextScope<ReadModelDbContext> scope, string attempt)
    {
        if (!await _tables.SetReplayAttempt(_replay, attempt, null, scope.DbContext))
        {
            throw new ReadModelTableNameBindingFailure(_replay.Owner, scope.DbContext.TableName, "the replay attempt changed during promotion");
        }

        await _tables.Transition(_replay, ReadModelTableBindingState.Ready, ReadModelTableBindingState.Reserved, scope.DbContext);
    }

    static Task<bool> SwapHasCommitted(ReadModelDbContext context, ReplayPromotion marker) =>
        context.ReplayPromotions.AnyAsync(promotion => promotion.Id == marker.Id
            && promotion.ContainerName == marker.ContainerName && promotion.RevertContainerName == marker.RevertContainerName);

    static async Task RecordPromotion(ReadModelDbContext context, ReplayPromotion marker)
    {
        // Retain older completion rows until their logical backups are pruned, but transfer ownership
        // of a colliding physical table. Pruning the old name must not drop the newly created backup.
        await context.ReplayPromotions.Where(promotion => promotion.BackupTableName == marker.BackupTableName)
            .ExecuteUpdateAsync(properties => properties.SetProperty(promotion => promotion.BackupTableName, (string?)null));
        context.ReplayPromotions.Add(marker);
        try
        {
            // The marker INSERT shares the DDL transaction. Neither can survive a rollback alone.
            await context.SaveChangesAsync();
        }
        finally
        {
            // A rolled-back attempt must insert again rather than retain EF's accepted/tracked state.
            context.Entry(marker).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Frees the live table's primary key name before the replay table is created. The live table of a
    /// database that swapped replays before primary keys followed their tables still holds that name.
    /// </summary>
    async Task NamePrimaryKeyAfterPrimaryTable()
    {
        await using var scope = await _tables.Open(_live, _columns);
        await PrimaryKeyConstraints.NameAfterTable(scope, scope.DbContext.TableName);
    }

    static async Task ExecuteDdl(DbContextScope<ReadModelDbContext> scope, string sql)
    {
#pragma warning disable EF1002 // sql is built from delimited identifiers via ISqlGenerationHelper.
        await scope.DbContext.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }

    static string BuildDropSql(DbContextScope<ReadModelDbContext> scope, string tableName)
    {
        var sqlHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        return $"DROP TABLE IF EXISTS {sqlHelper.DelimitIdentifier(tableName)}";
    }

    static string BuildRenameSql(DbContextScope<ReadModelDbContext> scope, DatabaseType databaseType, string oldName, string newName)
    {
        if (databaseType == DatabaseType.SqlServer)
        {
            // sp_rename takes name strings, not identifiers. The new name must be the unqualified
            // table name — Microsoft's docs are explicit on this.
            var oldEscaped = oldName.Replace("'", "''", StringComparison.Ordinal);
            var newEscaped = newName.Replace("'", "''", StringComparison.Ordinal);
            return $"EXEC sp_rename N'{oldEscaped}', N'{newEscaped}'";
        }

        var sqlHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        return $"ALTER TABLE {sqlHelper.DelimitIdentifier(oldName)} RENAME TO {sqlHelper.DelimitIdentifier(newName)}";
    }

    sealed record ReplayTablePromotion(
        string ReplayPhysical,
        string LivePhysical,
        string BackupPhysical,
        bool ReplayHasRows,
        ReplayPromotion Marker,
        ReadModelTableHandle Backup,
        bool BackupWasReady);
}
