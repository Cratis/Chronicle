// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public class a_replay_with_an_interleaved_reader : for_Sink.given.an_accumulating_sql_read_model<SqlSinkHarness>
{
    protected ISink _reader;
    protected Func<Task> _afterPrimaryRename = () => Task.CompletedTask;

    async Task Establish()
    {
        _reader = _sqlHarness.CreateSinkForTheSameReadModel();
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
    }

    protected override SqlSinkHarness CreateSqlHarness() => new()
    {
        Interceptors = [new after_primary_rename(() => _afterPrimaryRename())]
    };

    sealed class after_primary_rename(Func<Task> interleave) : DbCommandInterceptor
    {
        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText == $"ALTER TABLE \"{ContainerName}\" RENAME TO \"{ContainerName}-revert\"")
            {
                // Pause between the two real DDL commands, rather than hoping a poll hits the gap.
                await interleave();
            }

            return result;
        }
    }
}
