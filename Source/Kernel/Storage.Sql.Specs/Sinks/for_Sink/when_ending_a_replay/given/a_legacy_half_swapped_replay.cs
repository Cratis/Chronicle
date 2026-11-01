// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public abstract class a_legacy_half_swapped_replay<THarness> : for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected Exception? _error;
    protected bool _legacyAttemptInterrupted;
    protected int? _primary;
    protected int? _replay;
    protected int? _backup;

    half_swap _interceptor;

    protected abstract THarness CreateHarnessWithInterceptor(IInterceptor interceptor);

    protected override THarness CreateSqlHarness()
    {
        _interceptor = new half_swap();
        return CreateHarnessWithInterceptor(_interceptor);
    }

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        _interceptor.Armed = true;
        _legacyAttemptInterrupted = await Catch.Exception(() => _sink.EndReplay(ReplayContext())) is legacy_promotion_interrupted;
        await _sink.ResumeReplay(ReplayContext());
    }

    protected async Task RetryPromotion()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primary = await CurrentCountOrNull();
        _replay = await StoredCount($"replay-{ContainerName}");
        _backup = await StoredCount(ReplayContext().RevertContainerName.Value);
    }

    sealed class half_swap : DbCommandInterceptor
    {
        public bool Armed { get; set; }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (Armed && (command.CommandText == $"ALTER TABLE \"{ContainerName}\" RENAME TO \"{ContainerName}-revert\""
                || command.CommandText == $"ALTER TABLE {ContainerName} RENAME TO \"{ContainerName}-revert\""
                || command.CommandText == $"EXEC sp_rename N'{ContainerName}', N'{ContainerName}-revert'"))
            {
                Armed = false;

                // Reproduce a legacy database: primary -> revert committed, and an empty primary was
                // recreated. Copy its column shape without borrowing the backup's primary-key name.
                await eventData.Context!.Database.CurrentTransaction!.CommitAsync(cancellationToken);
                await using var recreate = command.Connection!.CreateCommand();
#pragma warning disable CA2100 // Only the specification's constant table identifiers.
                recreate.CommandText = eventData.Context.Database.IsSqlServer()
                    ? $"SELECT * INTO [{ContainerName}] FROM [{ContainerName}-revert] WHERE 1 = 0"
                    : $"CREATE TABLE \"{ContainerName}\" AS SELECT * FROM \"{ContainerName}-revert\" WHERE 1 = 0";
#pragma warning restore CA2100
                await recreate.ExecuteNonQueryAsync(cancellationToken);
                throw new legacy_promotion_interrupted();
            }

            return result;
        }
    }

    sealed class legacy_promotion_interrupted : Exception;
}
