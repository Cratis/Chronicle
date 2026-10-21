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
        var replayName = ReplayTableNameFor(_tableName);

        // Open the scope on the replay table so the DbContext we use for DDL is bound to
        // the rebuilt table. Other scopes may still be reading the primary table. The swap renames
        // tables, so it connects as the schema identity when connection resolution distinguishes one.
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, replayName, _columns, ConnectionPurpose.Schema);

        await scope.DbContext.EnsureReplayPromotions();
        var replayHasRows = await scope.DbContext.Entries.AsNoTracking().AnyAsync();
        var databaseType = scope.DbContext.Database.GetDatabaseType();
        var revertName = context.RevertContainerName.Value;
        var physicalRevert = PrimaryKeyNames.TableIdentifier(databaseType, revertName);
        var marker = new ReplayPromotion(ReplayPromotion.IdentifierFor(_tableName, revertName), _tableName, revertName, physicalRevert);
        var promotion = new ReplayTablePromotion(replayName, revertName, physicalRevert, databaseType, replayHasRows, marker);
        var recoveryName = $"chronicle_replay_backup_{Guid.NewGuid():N}";
        var preservedLegacyBackup = false;

        // Publish both renames and their constraint names in one transaction. Otherwise a concurrent
        // read can see the primary table missing and recreate it through EnsureTableExists, making
        // replay -> primary collide. A database transaction also protects readers on other silos
        // and restores the previous tables if any step of the promotion fails.
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

                preservedLegacyBackup = false;
                preservedLegacyBackup = await PromoteReplayTable(scope, promotion, recoveryName);
            },

            () => SwapHasCommitted(scope.DbContext, marker)));

        if (preservedLegacyBackup)
        {
            _logger.PreservedLegacyReplayBackup(_eventStoreName.Value, _namespace.Value, _tableName, physicalRevert, recoveryName);
        }
    }

    /// <summary>
    /// Renames the replay table into place inside the promotion transaction.
    /// </summary>
    /// <param name="scope">The <see cref="DbContextScope{TContext}"/> bound to the replay table.</param>
    /// <param name="promotion">The <see cref="ReplayTablePromotion"/> describing the tables involved.</param>
    /// <param name="recoveryName">The name a markerless legacy backup is preserved under.</param>
    /// <returns>True when a markerless legacy backup was preserved under the recovery name.</returns>
    /// <exception cref="ReplayTableBecameEmpty">The rebuilt table lost its rows before the promotion.</exception>
    /// <exception cref="UnverifiedReplayBackup">An existing backup cannot be distinguished from a legacy half-swap.</exception>
    async Task<bool> PromoteReplayTable(DbContextScope<ReadModelDbContext> scope, ReplayTablePromotion promotion, string recoveryName)
    {
        var (replayName, revertName, physicalRevert, databaseType, replayHasRows, marker) = promotion;
        if (!replayHasRows)
        {
            // No backup is touched on this path, so its ownership cannot block an empty replay.
            await ExecuteDdl(scope, BuildDropSql(scope, replayName));
            return false;
        }

        // Re-check on every attempt before touching the backup. A reader can recreate an empty
        // replay table after a committed swap; it must never replace the rebuilt primary.
        if (!await scope.DbContext.Entries.AsNoTracking().AnyAsync())
        {
            throw new ReplayTableBecameEmpty(replayName);
        }

        // Never move or replace the live or replay table because of identifier truncation.
        var namesCollide = physicalRevert == PrimaryKeyNames.TableIdentifier(databaseType, _tableName)
            || physicalRevert == PrimaryKeyNames.TableIdentifier(databaseType, replayName);
        if (namesCollide)
        {
            throw new UnverifiedReplayBackup(_tableName, physicalRevert);
        }

        var preservedLegacyBackup = await PreserveLegacyBackup(scope, promotion, recoveryName);

        // The old backup is absent, safely preserved, or owned by an earlier identified replay.
        await ExecuteDdl(scope, BuildDropSql(scope, revertName));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, _tableName, revertName));
        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, replayName, _tableName));

        // Constraint names must move in the same transaction, before the next replay is created.
        await PrimaryKeyConstraints.NameAfterTable(scope, revertName);
        await PrimaryKeyConstraints.NameAfterTable(scope, _tableName);
        await ReadModelIndexes.NameAfterTable(scope.DbContext, revertName);
        await ReadModelIndexes.NameAfterTable(scope.DbContext, _tableName);
        await RecordPromotion(scope.DbContext, marker);
        return preservedLegacyBackup;
    }

    async Task<bool> PreserveLegacyBackup(DbContextScope<ReadModelDbContext> scope, ReplayTablePromotion promotion, string recoveryName)
    {
        var database = scope.DbContext.Database;
        var (_, revertName, physicalRevert, databaseType, _, _) = promotion;
        if (!await TableExists(database, databaseType, revertName) ||
            await scope.DbContext.ReplayPromotions.AnyAsync(entry => entry.BackupTableName == physicalRevert && entry.ContainerName == _tableName))
        {
            return false;
        }

        // On upgrade a truncated name can collide with an old, markerless backup. Its
        // identity is unknowable, so preserve it, never drop it or invent a completion marker.
        // Exact-name half-swaps and tables owned by another read model still need recovery.
        if (await IsUnverifiableBackup(scope, promotion))
        {
            throw new UnverifiedReplayBackup(_tableName, physicalRevert);
        }

        await ExecuteDdl(scope, BuildRenameSql(scope, databaseType, physicalRevert, recoveryName));
        await PrimaryKeyConstraints.NameAfterTable(scope, recoveryName);
        return true;
    }

    async Task<bool> IsUnverifiableBackup(DbContextScope<ReadModelDbContext> scope, ReplayTablePromotion promotion)
    {
        var (_, revertName, physicalRevert, databaseType, _, _) = promotion;
        if (physicalRevert == revertName || !await TableExists(scope.DbContext.Database, databaseType, _tableName))
        {
            return true;
        }

        return await scope.DbContext.ReplayPromotions.AnyAsync(entry =>
            entry.BackupTableName == physicalRevert || entry.RevertContainerName == revertName);
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

    static Task<bool> TableExists(DatabaseFacade database, DatabaseType databaseType, string table)
    {
        var name = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var query = databaseType switch
        {
            DatabaseType.PostgreSql => database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_tables WHERE schemaname = current_schema() AND tablename = {name}"),
            DatabaseType.SqlServer => database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.tables WHERE schema_id = SCHEMA_ID() AND name = {name}"),
            _ => database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {name}")
        };
        return query.AnyAsync();
    }

    /// <summary>
    /// Frees the replay table's primary key name before the replay table is created. The primary table of a
    /// database that swapped replays before primary keys followed their tables still holds that name.
    /// </summary>
    async Task NamePrimaryKeyAfterPrimaryTable()
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        await PrimaryKeyConstraints.NameAfterTable(scope, _tableName);
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
        string ReplayName,
        string RevertName,
        string PhysicalRevert,
        DatabaseType DatabaseType,
        bool ReplayHasRows,
        ReplayPromotion Marker);
}
